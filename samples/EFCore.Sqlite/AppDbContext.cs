using Microsoft.EntityFrameworkCore;

namespace EFCore.Sqlite {
    public sealed class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            _ = modelBuilder.Entity<User>().HasKey(u => u.Id);
            _ = modelBuilder.Entity<User>().Property(u => u.Name).IsRequired();
            base.OnModelCreating(modelBuilder);
        }
    }
}
