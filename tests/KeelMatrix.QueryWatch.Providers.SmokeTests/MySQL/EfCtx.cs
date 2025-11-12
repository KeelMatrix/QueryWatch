using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.MySQL {
    internal sealed class EfCtx : DbContext {
        public EfCtx(DbContextOptions options) : base(options) { }
        public DbSet<Item> Items => Set<Item>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            _ = modelBuilder.Entity<Item>(e => {
                _ = e.ToTable("QW_Items");
                _ = e.HasKey(x => x.Id);
                _ = e.Property(x => x.Name)
                    .HasMaxLength(200);
            });
        }
    }
}
