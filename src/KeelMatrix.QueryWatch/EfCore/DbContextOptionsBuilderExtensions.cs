#if NET8_0
#nullable enable
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore {
    /// <summary>
    /// EF Core integration helpers for wiring QueryWatch into a DbContext.
    /// </summary>
    public static class DbContextOptionsBuilderExtensions {
        // TODO: wierd characters in XML comments

        /// <summary>
        /// Attach QueryWatch interceptor to a DbContextOptionsBuilder.
        /// </summary>
        /// <example>
        /// Typical usage in tests:
        /// <code>
        /// using var session = QueryWatcher.Start(new QueryWatchOptions { MaxQueries = 5 });
        /// var opts = new DbContextOptionsBuilder&lt;MyDbContext&gt;()
        ///     .UseInMemoryDatabase("test")
        ///     .AddInterceptors(new EfCoreQueryWatchInterceptor(session))
        ///     .Options;
        /// using var db = new MyDbContext(opts);
        /// // run code under test...
        /// var report = session.Stop().ShouldHaveExecutedAtMost(5);
        /// </code>
        /// </example>
        public static DbContextOptionsBuilder UseQueryWatch(
            this DbContextOptionsBuilder builder,
            QueryWatchSession session) {
            builder.AddInterceptors(new EfCoreQueryWatchInterceptor(session));
            return builder;
        }
    }
}
#endif
