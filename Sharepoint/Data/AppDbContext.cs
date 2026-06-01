using Microsoft.EntityFrameworkCore;
using Sharepoint.Models;

namespace Sharepoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<WorkCategory> WorkCategories => Set<WorkCategory>();
        public DbSet<WorkDocument> WorkDocuments => Set<WorkDocument>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<UserFavorite> UserFavorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Username).IsUnique();
            });

            modelBuilder.Entity<UserFavorite>(entity =>
            {
                entity.HasKey(uf => uf.Id);

                entity.HasOne(uf => uf.AppUser)
                      .WithMany()
                      .HasForeignKey(uf => uf.AppUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uf => uf.WorkCategory)
                      .WithMany()
                      .HasForeignKey(uf => uf.WorkCategoryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(uf => new { uf.AppUserId, uf.WorkCategoryId }).IsUnique();
            });
        }
    }
}