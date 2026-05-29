using Microsoft.EntityFrameworkCore;
using Sharepoint.Models;
using SkiaSharp;
using System.Collections.Generic;

namespace Sharepoint.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleFile> ModuleFiles { get; set; }
    }
}