using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.MySQL {
    internal sealed class EfCtx : DbContext {
        public EfCtx(DbContextOptions options) : base(options) { }
        public DbSet<Item> Items => Set<Item>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Item>(e => {
                e.ToTable("QW_Items");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name)
                    .HasMaxLength(200);
            });
        }
    }
}
