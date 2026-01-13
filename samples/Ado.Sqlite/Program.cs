// Copyright (c) KeelMatrix

using Ado.Sqlite;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Reporting;
using Microsoft.Data.Sqlite;

// Plain ADO.NET + SQLite
string artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts");
Directory.CreateDirectory(artifacts);
string outJson = Path.Combine(artifacts, "qwatch.ado.json");

// Configure session options
QueryWatchOptions options = new() {
    MaxQueries = 50,
    MaxAverageDuration = TimeSpan.FromMilliseconds(200)
};

// Start a QueryWatch session
using QueryWatchSession session = QueryWatcher.Start(options);

// In-memory SQLite needs the connection to stay open for the DB to persist.
using SqliteConnection raw = new("Data Source=:memory:");
await raw.OpenAsync();

// Wrap the provider connection so all commands record into the QueryWatch session.
using QueryWatchConnection conn = new(raw, session);

// Create schema (we include a harmless SQL comment with an email to show masking)
using (var cmd = conn.CreateCommand()) {
    cmd.CommandText = Redaction.Apply("/* contact: admin@example.com */ CREATE TABLE Users(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);");
    _ = await cmd.ExecuteNonQueryAsync();
}

// Insert a few rows (we also redact parameter strings defensively)
for (int i = 0; i < 5; i++) {
    using var ins = conn.CreateCommand();
    string email = $"user{i}@example.com"; // demo PII-like value (will be masked in CommandText)
    ins.CommandText = Redaction.Apply($"/* email: {email} */ INSERT INTO Users(Name) VALUES ($name);");

    var p = ins.CreateParameter();
    p.ParameterName = "$name";
    p.Value = Redaction.Param("User_" + i); // if your JSON ever includes parameters, this stays safe
    _ = ins.Parameters.Add(p);

    _ = await ins.ExecuteNonQueryAsync();
}

// Query back
using (var select = conn.CreateCommand()) {
    select.CommandText = Redaction.Apply("SELECT COUNT(*) FROM Users WHERE Name LIKE 'User_%';");
    int count = Convert.ToInt32(await select.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    Console.WriteLine($"Users in DB: {count}");
}

// Stop session and produce report
QueryWatchReport report = session.Stop();

// Export JSON
QueryWatchJson.ExportToFile(report, outJson, sampleTop: 50);

// Enforce budgets
report.ThrowIfViolations();

Console.WriteLine($"QueryWatch JSON written to: {outJson}");
Console.WriteLine("Try the CLI gate:");
Console.WriteLine($"  dotnet run --project ../../tools/KeelMatrix.QueryWatch.Cli -- --input '{outJson}' --max-queries 50");
