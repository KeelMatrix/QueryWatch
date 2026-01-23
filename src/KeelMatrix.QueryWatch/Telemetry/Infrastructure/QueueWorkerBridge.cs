// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Helper used to wire queue enqueue operations to the background worker.
    /// Existing code can call this without blocking.
    /// </summary>
    internal static class QueueWorkerBridge {
        public static void Enqueue(string payloadJson) {
            TelemetryDeliveryWorker.EnsureStarted();
            DurableTelemetryQueue.Instance.Enqueue(payloadJson);
            TelemetryDeliveryWorker.NotifyNewItem();
        }
    }
}
