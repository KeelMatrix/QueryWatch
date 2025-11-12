// Copyright (c) KeelMatrix
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class TestDbContext : DbContext {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Thing> Things => Set<Thing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            _ = modelBuilder.Entity<Thing>(b => {
                _ = b.ToTable("Things");
                _ = b.HasKey(e => e.Id);
                _ = b.Property(e => e.Name).HasMaxLength(200);
            });
        }
    }

    public sealed class Thing {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
