// Copyright (c) KeelMatrix

using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch {
    internal static class QueryWatchTelemetry {
        private static readonly Client Client = new("QueryWatch", typeof(QueryWatchSession));

        internal static void TrackActivation() => Client.TrackActivation();

        internal static void TrackHeartbeat() => Client.TrackHeartbeat();
    }
}
