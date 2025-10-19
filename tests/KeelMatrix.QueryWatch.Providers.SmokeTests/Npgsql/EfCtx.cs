using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.Npgsql {
    internal sealed class EfCtx : DbContext {
        public EfCtx(DbContextOptions options) : base(options) { }
        public DbSet<Item> Items => Set<Item>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Item>(e => {
                e.ToTable("qw_items");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(200);
            });
        }
    }
}
