// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    internal interface ITelemetryQueue {
        void Enqueue(string payloadJson);
        IEnumerable<DurableTelemetryQueue.ClaimedItem> TryClaim(int maxItems);
        void Abandon(DurableTelemetryQueue.ClaimedItem item);
        void Complete(DurableTelemetryQueue.ClaimedItem item);
    }

    internal sealed class NullTelemetryQueue : ITelemetryQueue {
        public void Enqueue(string payloadJson) { }
        public IEnumerable<DurableTelemetryQueue.ClaimedItem> TryClaim(int maxItems) { yield break; }
        public void Abandon(DurableTelemetryQueue.ClaimedItem item) { }
        public void Complete(DurableTelemetryQueue.ClaimedItem item) { }
    }
}
