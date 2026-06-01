// Controllers/FavoritesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Data;
using Sharepoint.Models;

namespace Sharepoint.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _db;

        public FavoritesController(AppDbContext db)
        {
            _db = db;
        }

        // Returns the current user's ID from the DB.
        // Adjust if you store userId differently (e.g. claims).
        private int? GetCurrentUserId()
        {
            var username = User.Identity?.Name;
            if (username == null) return null;
            return _db.Users.FirstOrDefault(u => u.Username == username)?.Id;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int categoryId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var existing = _db.UserFavorites
                .FirstOrDefault(f => f.AppUserId == userId && f.WorkCategoryId == categoryId);

            bool isFavorited;
            if (existing != null)
            {
                _db.UserFavorites.Remove(existing);
                isFavorited = false;
            }
            else
            {
                _db.UserFavorites.Add(new UserFavorite
                {
                    AppUserId = userId.Value,
                    WorkCategoryId = categoryId
                });
                isFavorited = true;
            }

            _db.SaveChanges();

            // Return JSON so the JS can update the star without a page reload
            return Json(new { isFavorited });
        }
    }
}