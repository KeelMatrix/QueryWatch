// Copyright (c) KeelMatrix

using EFCore.Sqlite;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.EfCore;
using KeelMatrix.QueryWatch.Reporting;
using Microsoft.EntityFrameworkCore;

// EF Core + SQLite sample
string artifacts = Path.Combine(AppContext.BaseDirectory, "artifacts");
Directory.CreateDirectory(artifacts);
string outJson = Path.Combine(artifacts, "qwatch.ef.json");

// Configure session options
QueryWatchOptions options = new() {
    MaxQueries = 50, // keep generous to avoid failing demo runs
    MaxAverageDuration = TimeSpan.FromMilliseconds(200)
};

// Start a QueryWatch session
using QueryWatchSession session = QueryWatcher.Start(options);

string dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
DbContextOptions<AppDbContext> dbOptions = (DbContextOptions<AppDbContext>)new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .UseQueryWatch(session) // wires the EF Core interceptor
    .Options;

using (AppDbContext db = new(dbOptions)) {
    _ = await db.Database.EnsureDeletedAsync();
    _ = await db.Database.EnsureCreatedAsync();

    // seed a bit of data
    db.Users.AddRange(
        new User { Name = "Alice" },
        new User { Name = "Bob" },
        new User { Name = "Charlie" },
        new User { Name = "Diana" }
    );
    _ = await db.SaveChangesAsync();

    // We purposefully run one raw SQL that starts with "SELECT * FROM Users"
    // so CLI pattern budgets like --budget "SELECT * FROM Users*=1" match predictably.
    var predictable = await db.Users
        .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE 'A%'") // exact text you want to budget
        .AsNoTracking()
        .ToListAsync();
    Console.WriteLine($"Pattern budget demo rows (Name LIKE 'A%'): {predictable.Count}");

    // a few queries
    int total = await db.Users.CountAsync();
    var likeA = await db.Users.Where(u => EF.Functions.Like(u.Name, "%a%")).ToListAsync();
    var first = await db.Users.OrderBy(u => u.Id).FirstAsync();

    Console.WriteLine($"Users: {total}, Like 'a': {likeA.Count}, First: {first.Name}");
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
