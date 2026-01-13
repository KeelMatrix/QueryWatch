// Copyright (c) KeelMatrix

using Dapper;
using DapperSample;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Reporting;
using Microsoft.Data.Sqlite;

// Dapper + SQLite sample (async + transactions)
string artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts");
Directory.CreateDirectory(artifacts);
string outJson = Path.Combine(artifacts, "qwatch.dapper.json");

// Configure session options
QueryWatchOptions options = new() {
    MaxQueries = 50,
    MaxAverageDuration = TimeSpan.FromMilliseconds(200)
};

// Start a QueryWatch session
using QueryWatchSession session = QueryWatcher.Start(options);

// Create an in-memory DB
using SqliteConnection raw = new("Data Source=:memory:");
await raw.OpenAsync();

// Wrap with QueryWatch (returns QueryWatchConnection under the hood for SQLite)
using var conn = raw.WithQueryWatch(session);

// Create a table
await conn.ExecuteAsync(
    Redaction.Apply("/* contact: admin@example.com */ CREATE TABLE Users(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);")
);

// Insert in a transaction (exercise Transaction wrapper + async APIs)
using (var tx = await conn.BeginTransactionAsync()) {
    for (int i = 0; i < 3; i++) {
        string email = $"user{i}@example.com"; // will be redacted in CommandText
        _ = await conn.ExecuteAsync(
            Redaction.Apply($"/* email: {email} */ INSERT INTO Users(Name) VALUES (@name);"),
            new { name = Redaction.Param("User_" + i) },
            transaction: tx);
    }
    await tx.CommitAsync();
}

// Query back (async)
int total = await conn.ExecuteScalarAsync<int>(
    Redaction.Apply("SELECT COUNT(*) FROM Users WHERE Name LIKE 'User_%';")
);
Console.WriteLine($"Users in DB: {total}");

// Stop session and produce report
QueryWatchReport report = session.Stop();

// Export JSON
QueryWatchJson.ExportToFile(report, outJson, sampleTop: 50);

// Enforce budgets
report.ThrowIfViolations();

Console.WriteLine($"QueryWatch JSON written to: {outJson}");
Console.WriteLine("Try the CLI gate:");
Console.WriteLine($"  dotnet run --project ../../tools/KeelMatrix.QueryWatch.Cli -- --input '{outJson}' --max-queries 50");
