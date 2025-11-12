using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.Npgsql {
    internal sealed class EfCtx : DbContext {
        public EfCtx(DbContextOptions options) : base(options) { }
        public DbSet<Item> Items => Set<Item>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            _ = modelBuilder.Entity<Item>(e => {
                _ = e.ToTable("qw_items");
                _ = e.HasKey(x => x.Id);
                _ = e.Property(x => x.Id)
                    .HasColumnName("id");
                _ = e.Property(x => x.Name)
                    .HasColumnName("name")
                    .HasMaxLength(200);
            });
        }
    }
}
