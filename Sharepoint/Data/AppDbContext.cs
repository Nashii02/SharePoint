using Microsoft.EntityFrameworkCore;
using Sharepoint.Models;

namespace Sharepoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<WorkDocument> WorkDocuments { get; set; }
        public DbSet<WorkCategory> WorkCategories { get; set; }
    }
}