namespace KeelMatrix.QueryWatch.Cli.Core {
    public static class ExitCodes {
        public const int Ok = 0;
        public const int InvalidArguments = 1;
        public const int InputFileNotFound = 2;
        public const int JsonParseError = 3;
        public const int BudgetExceeded = 4;
        public const int BaselineRegression = 5;
    }
}
