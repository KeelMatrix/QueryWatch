// Copyright (c) KeelMatrix

using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch.Cli {
    internal interface ITelemetryHost {
        void TrackActivation();
    }

    internal sealed class TelemetryHostClient : ITelemetryHost {
        private static readonly Client Client = new("qwatchCLI", typeof(Program));

        public void TrackActivation() {
            Client.TrackActivation();
        }
    }

    internal static class TelemetryHost {
        private static ITelemetryHost current = new TelemetryHostClient();

        internal static void TrackActivation() {
            Volatile.Read(ref current).TrackActivation();
        }

        internal static ITelemetryHost Current {
            get => Volatile.Read(ref current);
            set => Volatile.Write(ref current, value ?? throw new ArgumentNullException(nameof(value)));
        }
    }
}
