using Dapper;
using DapperSample;
using KeelMatrix.QueryWatch.Dapper;
using KeelMatrix.QueryWatch.Testing;
using Microsoft.Data.Sqlite;

// Dapper + SQLite sample (async + transactions)
var artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts");
Directory.CreateDirectory(artifacts);
var outJson = Path.Combine(artifacts, "qwatch.dapper.json");

using var q = QueryWatchScope.Start(
    maxQueries: 50,
    maxAverage: TimeSpan.FromMilliseconds(200),
    exportJsonPath: outJson,
    sampleTop: 50);

// Create an in-memory DB
using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

// Wrap with QueryWatch (returns QueryWatchConnection under the hood for SQLite)
using var conn = raw.WithQueryWatch(q.Session);

// Create a table
await conn.ExecuteAsync(Redaction.Apply("/* contact: admin@example.com */ CREATE TABLE Users(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);"));

// Insert in a transaction (exercise Transaction wrapper + async APIs)
using (var tx = conn.BeginTransaction()) {
    for (int i = 0; i < 3; i++) {
        var email = $"user{i}@example.com"; // will be redacted in CommandText
        await conn.ExecuteAsync(
            Redaction.Apply($"/* email: {email} */ INSERT INTO Users(Name) VALUES (@name);"),
            new { name = Redaction.Param("User_" + i) },
            transaction: tx);
    }
    tx.Commit();
}

// Query back (async)
var total = await conn.ExecuteScalarAsync<int>(Redaction.Apply("SELECT COUNT(*) FROM Users WHERE Name LIKE 'User_%';"));
Console.WriteLine($"Users in DB: {total}");

Console.WriteLine($"QueryWatch JSON written to: {outJson}");
Console.WriteLine("Try the CLI gate:");
Console.WriteLine($"  dotnet run --project ../../tools/KeelMatrix.QueryWatch.Cli -- --input '{outJson}' --max-queries 50");

