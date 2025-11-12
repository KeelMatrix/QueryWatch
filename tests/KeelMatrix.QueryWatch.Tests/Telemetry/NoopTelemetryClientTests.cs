using FluentAssertions;
using KeelMatrix.QueryWatch.Telemetry;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Telemetry {
    public class NoopTelemetryClientTests {
        [Fact]
        public void TrackActivation_DoesNotThrow() {
            NoopTelemetryClient client = new();
            _ = client.Invoking(c => c.TrackActivation()).Should().NotThrow();
        }

        [Fact]
        public void TrackHeartbeat_DoesNotThrow() {
            NoopTelemetryClient client = new();
            _ = client.Invoking(c => c.TrackHeartbeat()).Should().NotThrow();
        }
    }
}
