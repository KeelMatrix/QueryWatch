// Copyright (c) KeelMatrix
#nullable enable
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class TestDbContext : DbContext {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Thing> Things => Set<Thing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Thing>(b => {
                b.ToTable("Things");
                b.HasKey(e => e.Id);
                b.Property(e => e.Name).HasMaxLength(200);
            });
        }
    }

    public sealed class Thing {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
