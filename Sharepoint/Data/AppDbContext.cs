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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique username index
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}