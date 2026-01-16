// Copyright (c) KeelMatrix

using System.Data;
using System.Data.Common;

namespace KeelMatrix.QueryWatch.Infrastructure {
    internal interface IConnectionInstrumenter {
        DbConnection Wrap(DbConnection connection, QueryWatchSession session);
        IDbConnection Wrap(IDbConnection connection, QueryWatchSession session);
    }
}
