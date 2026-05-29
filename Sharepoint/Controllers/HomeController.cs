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
            var categories = await _db.WorkCategories
                .OrderBy(c => c.IsDefault ? 0 : 1)
                .ThenBy(c => c.CreatedAt)
                .ToListAsync();
            ViewBag.Categories = categories;
            return View();
        }
    }
}