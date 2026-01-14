// Copyright (c) KeelMatrix

using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchScopeEnforcementTests {
        [Fact]
        public void Dispose_Enforces_MaxQueries() {
            using QueryWatchSession session = new();
            using var scope = new TestQueryWatchScope(session, maxQueries: 1);

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(1));

            Action act = scope.Dispose;
            _ = act.Should().Throw<QueryWatchViolationException>();
        }

        [Fact]
        public void Dispose_Enforces_MaxAverage() {
            using QueryWatchSession session = new();
            using TestQueryWatchScope scope = new(session, maxAverage: TimeSpan.FromMilliseconds(5));

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(8));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(4));

            Action act = scope.Dispose;
            _ = act.Should().Throw<QueryWatchViolationException>();
        }

        [Fact]
        public void Dispose_Enforces_MaxTotal() {
            using QueryWatchSession session = new();
            using TestQueryWatchScope scope = new(session, maxTotal: TimeSpan.FromMilliseconds(10));

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(6));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(5));

            Action act = scope.Dispose;
            _ = act.Should().Throw<QueryWatchViolationException>();
        }
    }
}
