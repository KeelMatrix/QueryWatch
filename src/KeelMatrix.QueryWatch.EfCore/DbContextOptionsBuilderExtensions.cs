// Copyright (c) KeelMatrix
#nullable enable
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore {
    /// <summary>
    /// EF Core integration helpers for wiring QueryWatch into a <see cref="DbContext"/>.
    /// </summary>
    public static class DbContextOptionsBuilderExtensions {
        /// <summary>
        /// Attaches the QueryWatch command interceptor to the supplied EF Core options builder
        /// so that all executed database commands are recorded into the given session.
        /// </summary>
        /// <param name="builder">The <see cref="DbContextOptionsBuilder"/> to augment.</param>
        /// <param name="session">The active <see cref="QueryWatchSession"/> to record into.</param>
        /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
        public static DbContextOptionsBuilder UseQueryWatch(
            this DbContextOptionsBuilder builder,
            QueryWatchSession session) {
            builder.AddInterceptors(new EfCoreQueryWatchInterceptor(session));
            return builder;
        }
    }
}
