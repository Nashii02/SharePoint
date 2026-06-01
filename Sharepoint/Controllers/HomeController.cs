using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sharepoint.Data;
using Sharepoint.Models;
using System.Diagnostics;

namespace Sharepoint.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> WorkInstruction()
        {
            var categories = await _db.WorkCategories.ToListAsync();
            var docCounts = await _db.WorkDocuments
                .GroupBy(d => d.Category)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Map slug-based docCounts to category ID-based
            ViewBag.Categories = categories;
            ViewBag.DocCounts = categories.ToDictionary(
                c => c.Id,
                c => docCounts.TryGetValue(c.Slug, out var count) ? count : 0
            );

            // ? THIS is what's missing
            var favoriteIds = new HashSet<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    favoriteIds = (await _db.UserFavorites
                        .Where(f => f.AppUserId == user.Id)
                        .Select(f => f.WorkCategoryId)
                        .ToListAsync())
                        .ToHashSet();
                }
            }
            ViewBag.FavoriteIds = favoriteIds;

            return View();
        }
    }
}