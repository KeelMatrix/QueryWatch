// Copyright (c) KeelMatrix

using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch.Cli {
    internal static class QueryWatchCliTelemetry {
        private static readonly Client Client = new("qwatchCLI", typeof(Program));

        internal static void TrackActivation() => Client.TrackActivation();
    }
}
