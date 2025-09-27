#nullable enable
using FluentAssertions;
using KeelMatrix.QueryWatch.Telemetry;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Telemetry {
    public class NoopTelemetryClientTests {
        [Fact]
        public void TrackActivation_DoesNotThrow() {
            var client = new NoopTelemetryClient();
            client.Invoking(c => c.TrackActivation()).Should().NotThrow();
        }

        [Fact]
        public void TrackHeartbeat_DoesNotThrow() {
            var client = new NoopTelemetryClient();
            client.Invoking(c => c.TrackHeartbeat()).Should().NotThrow();
        }
    }
}
