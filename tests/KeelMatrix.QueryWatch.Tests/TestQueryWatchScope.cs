// Copyright (c) KeelMatrix

using KeelMatrix.QueryWatch.Reporting;

namespace KeelMatrix.QueryWatch.Tests;

/// <summary>
/// Test-only disposable helper.
/// Owns a real QueryWatchSession and enforces expectations on dispose.
/// </summary>
internal sealed class TestQueryWatchScope : IDisposable {
    private bool _disposed;

    public TestQueryWatchScope(
        QueryWatchSession session,
        int? maxQueries = null,
        TimeSpan? maxAverage = null,
        TimeSpan? maxTotal = null,
        string? exportJsonPath = null,
        int sampleTop = 5) {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        MaxQueries = maxQueries;
        MaxAverage = maxAverage;
        MaxTotal = maxTotal;
        ExportJsonPath = exportJsonPath;
        SampleTop = sampleTop;
    }

    public QueryWatchSession Session { get; }

    private int? MaxQueries { get; }
    private TimeSpan? MaxAverage { get; }
    private TimeSpan? MaxTotal { get; }
    private string? ExportJsonPath { get; }
    private int SampleTop { get; }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;

        QueryWatchReport report = Session.Stop();

        if (!string.IsNullOrWhiteSpace(ExportJsonPath)) {
            try {
                QueryWatchJson.ExportToFile(report, ExportJsonPath!, SampleTop);
            }
            catch {
                // Swallow export errors so budget failures still surface
            }
        }

        if (MaxQueries.HasValue)
            report.ShouldHaveExecutedAtMost(MaxQueries.Value);

        if (MaxAverage.HasValue)
            report.ShouldHaveMaxAverageTime(MaxAverage.Value);

        if (MaxTotal.HasValue)
            report.ShouldHaveMaxTotalTime(MaxTotal.Value);
    }
}
