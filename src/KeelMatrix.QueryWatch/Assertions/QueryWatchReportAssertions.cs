// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Assertions {
    /// <summary>
    /// Provides fluent assertion helpers for <see cref="QueryWatchReport"/>.
    /// </summary>
    /// <remarks>
    ///
    /// <para>
    /// These extensions are intended for use in tests and validation code to express expectations
    /// over query execution behavior in a readable, chainable form.
    /// </para>
    ///
    /// <para>
    /// The methods in this class throw <see cref="QueryWatchViolationException"/> when an assertion
    /// fails and return the original <see cref="QueryWatchReport"/> when successful, enabling
    /// fluent chaining.
    /// </para>
    ///
    /// <para>
    /// This namespace is deliberately separate from the core QueryWatch model to keep the primary
    /// API surface minimal while offering optional ergonomics for test and CI scenarios.
    /// </para>
    /// </remarks>
    public static class QueryWatchReportAssertions {
        /// <summary>
        /// Asserts that at most <paramref name="maxQueries"/> were executed.
        /// </summary>
        /// <param name="report">The report to assert against.</param>
        /// <param name="maxQueries">Maximum allowed queries.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public static QueryWatchReport ShouldHaveExecutedAtMost(this QueryWatchReport report, int maxQueries) {
            return report.TotalQueries > maxQueries
                ? throw new QueryWatchViolationException($"Expected ≤{maxQueries} queries, but executed {report.TotalQueries}.")
                : report;
        }

        /// <summary>
        /// Asserts that the average query time does not exceed <paramref name="maxAverage"/>.
        /// </summary>
        /// <param name="report">The report to assert against.</param>
        /// <param name="maxAverage">Maximum allowed average time.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public static QueryWatchReport ShouldHaveMaxAverageTime(this QueryWatchReport report, TimeSpan maxAverage) {
            return report.AverageDuration > maxAverage
                ? throw new QueryWatchViolationException($"Expected average ≤{maxAverage}, actual {report.AverageDuration}.")
                : report;
        }

        /// <summary>
        /// Asserts that the total query time does not exceed <paramref name="maxTotal"/>.
        /// </summary>
        /// <param name="report">The report to assert against.</param>
        /// <param name="maxTotal">Maximum allowed total time.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public static QueryWatchReport ShouldHaveMaxTotalTime(this QueryWatchReport report, TimeSpan maxTotal) {
            return report.TotalDuration > maxTotal
                ? throw new QueryWatchViolationException($"Expected total ≤{maxTotal}, actual {report.TotalDuration}.")
                : report;
        }
    }
}
