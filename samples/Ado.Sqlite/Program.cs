using Ado.Sqlite;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Testing;
using Microsoft.Data.Sqlite;

// Plain ADO.NET + SQLite sample
var artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts");
Directory.CreateDirectory(artifacts);
var outJson = Path.Combine(artifacts, "qwatch.ado.json");

using var q = QueryWatchScope.Start(
    maxQueries: 50,
    maxAverage: TimeSpan.FromMilliseconds(200),
    exportJsonPath: outJson,
    sampleTop: 50);

// In-memory SQLite needs the connection to stay open for the DB to persist.
using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

// Wrap the provider connection so all commands record into the QueryWatch session.
using var conn = new QueryWatchConnection(raw, q.Session);

// Create schema (we include a harmless SQL comment with an email to show masking)
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = Redaction.Apply("/* contact: admin@example.com */ CREATE TABLE Users(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);");
    await cmd.ExecuteNonQueryAsync();
}

// Insert a few rows (we also redact parameter strings defensively)
for (int i = 0; i < 5; i++) {
    using var ins = conn.CreateCommand();
    var email = $"user{i}@example.com"; // demo PII-like value (will be masked in CommandText)
    ins.CommandText = Redaction.Apply($"/* email: {email} */ INSERT INTO Users(Name) VALUES ($name);");

    var p = ins.CreateParameter();
    p.ParameterName = "$name";
    p.Value = Redaction.Param("User_" + i); // if your JSON ever includes parameters, this stays safe
    ins.Parameters.Add(p);

    await ins.ExecuteNonQueryAsync();
}

// Query back
using (var select = conn.CreateCommand()) {
    select.CommandText = Redaction.Apply("SELECT COUNT(*) FROM Users WHERE Name LIKE 'User_%';");
    var count = Convert.ToInt32(await select.ExecuteScalarAsync());
    Console.WriteLine($"Users in DB: {count}");
}

Console.WriteLine($"QueryWatch JSON written to: {outJson}");
Console.WriteLine("Try the CLI gate:");
Console.WriteLine($"  dotnet run --project ../../tools/KeelMatrix.QueryWatch.Cli -- --input {outJson} --max-queries 50");
