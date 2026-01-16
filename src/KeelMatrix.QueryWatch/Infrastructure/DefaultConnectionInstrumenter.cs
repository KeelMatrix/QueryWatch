// Copyright (c) KeelMatrix

using System.Data;
using System.Data.Common;
using KeelMatrix.QueryWatch.Infrastructure.Ado;
using KeelMatrix.QueryWatch.Infrastructure.Dapper;

namespace KeelMatrix.QueryWatch.Infrastructure {
    internal sealed class DefaultConnectionInstrumenter : IConnectionInstrumenter {
        public DbConnection Wrap(DbConnection connection, QueryWatchSession session) {
            if (connection is null) {
                throw new ArgumentNullException(nameof(connection));
            }

            if (session is null) {
                throw new ArgumentNullException(nameof(session));
            }

            return new InstrumentedDbConnection(connection, session);
        }

        public IDbConnection Wrap(IDbConnection connection, QueryWatchSession session) {
            if (connection is null) {
                throw new ArgumentNullException(nameof(connection));
            }

            if (session is null) {
                throw new ArgumentNullException(nameof(session));
            }

            if (connection is DbConnection db) {
                return new InstrumentedDbConnection(db, session);
            }

            return new InstrumentedIdbConnection(connection, session);
        }
    }
}
