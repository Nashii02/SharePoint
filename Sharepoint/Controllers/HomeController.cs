using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Data;
using Sharepoint.Models;
using Microsoft.AspNetCore.Authorization;


namespace Sharepoint.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;


        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction("WorkInstruction");
        }




        [AllowAnonymous]
        // shows all module cards
        public IActionResult WorkInstruction()
        {
            var allModules = _context.Modules.ToList();

            var recentCutoff = DateTime.Now.AddDays(-7);  // new modules show for 7 days

            // Pinned modules (IsFeatured = true)
            var pinnedModules = allModules
                .Where(m => m.IsFeatured)
                .ToList();

            // New modules added within last 7 days (not pinned)
            var newModules = allModules
                .Where(m => !m.IsFeatured && m.CreatedAt.HasValue && m.CreatedAt >= recentCutoff)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            // Recently updated modules (not pinned, not new)
            var recentlyUpdated = allModules
                .Where(m => !m.IsFeatured
                         && m.LastFileUpload.HasValue
                         && !(m.CreatedAt.HasValue && m.CreatedAt >= recentCutoff))
                .OrderByDescending(m => m.LastFileUpload)
                .Take(9)
                .ToList();

            // Combine all featured: pinned + new + recently updated
            var featuredModules = pinnedModules
                .Concat(newModules)
                .Concat(recentlyUpdated)
                .DistinctBy(m => m.Slug)
                .ToList();

            var model = new WorkInstructionViewModel
            {
                TotalModules = allModules.Count,
                TotalCategories = allModules
                    .Where(m => !m.IsFeatured)
                    .GroupBy(m => m.Category ?? "Uncategorized")
                    .Count(),
                RecentlyUpdatedCount = recentlyUpdated.Count,

                FeaturedCards = featuredModules
                    .Select(m => new WorkInstructionCard
                    {
                        Title = m.Title,
                        Subtitle = m.Subtitle ?? "",
                        IconClass = m.IconClass ?? "",

                        // Badge depends on why it's featured
                        Status = m.IsFeatured ? "Pinned"
                               : (m.CreatedAt.HasValue && m.CreatedAt >= recentCutoff) ? "New Module"
                               : "Recently Updated",

                        StatusColor = m.IsFeatured ? "pinned"
                                    : (m.CreatedAt.HasValue && m.CreatedAt >= recentCutoff) ? "new"
                                    : "updated",

                        LastUpdated = m.CreatedAt.HasValue && m.CreatedAt >= recentCutoff
                                    ? m.CreatedAt?.ToString("MMM dd, yyyy") ?? ""
                                    : m.LastFileUpload?.ToString("MMM dd, yyyy") ?? "",

                        URL = $"/Home/Module?id={m.Slug}"
                    }).ToList(),

                // Secondary = ALL modules regardless of pin (shows everything)
                GroupedSecondaryCards = allModules
                    .GroupBy(m => m.Category ?? "Uncategorized")
                    .OrderBy(g => g.Key == "Uncategorized" ? "zzz" : g.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(m => new WorkInstructionCard
                        {
                            Title = m.Title,
                            Subtitle = m.Subtitle ?? "",
                            IconClass = m.IconClass ?? "",
                            Category = m.Category ?? "Uncategorized",
                            CategoryColor = m.CategoryColor,
                            IsFeatured = m.IsFeatured,
                            URL = $"/Home/Module?id={m.Slug}"
                        }).ToList()
                    )
            };

            return View(model);
        }



        [AllowAnonymous]
        // MODULE PAGE — shows files for a specific module
        public IActionResult Module(string id)
        {
            var module = _context.Modules.FirstOrDefault(m => m.Slug == id);

            var model = new ModulePageViewModel
            {
                ModuleTitle = module?.Title ?? "Module",
                ModuleSubtitle = module?.Subtitle ?? "Work instruction module",
                ModuleSlug = id ?? "",    // ← this must be set

                Files = _context.ModuleFiles
                            .Where(f => f.ModuleSlug == id && !f.IsDeleted)
                            .ToList(),

                DeletedFiles = _context.ModuleFiles
                            .Where(f => f.ModuleSlug == id && f.IsDeleted)
                            .OrderByDescending(f => f.DeletedAt)
                            .ToList()
            };

            return View("Module", model);
        }




        [Authorize(Roles = "Admin, User")]
        // UPLOAD FILE — saves file to folder and records it in DB
        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string moduleId, string description)
        {
            if (string.IsNullOrEmpty(moduleId))
                return BadRequest("Module ID is missing.");

            if (file == null || file.Length == 0)
                return BadRequest("No file selected.");


            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId);
            Directory.CreateDirectory(uploadsFolder);

            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLower();

            // Check if a file with the same original name already exists
            var existingVersions = _context.ModuleFiles
                .Where(f => f.ModuleSlug == moduleId && f.OriginalFileName == originalName)
                .ToList();

            var newVersion = existingVersions.Any()
                ? existingVersions.Max(f => f.Version) + 1
                : 1;

            // Save with version number in filename to avoid overwriting
            var versionedFileName = newVersion == 1
                ? file.FileName
                : $"{originalName}_v{newVersion}.{ext}";

            var filePath = Path.Combine(uploadsFolder, versionedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            var bytes = file.Length;
            string fileSize = bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
                _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
            };

            _context.ModuleFiles.Add(new ModuleFile
            {
                ModuleSlug = moduleId,
                FileName = Path.GetFileNameWithoutExtension(versionedFileName),
                OriginalFileName = originalName,
                FileType = ext,
                FilePath = filePath,
                FileUrl = $"/uploads/{moduleId}/{versionedFileName}",
                UploadedDate = DateTime.Now,
                Description = description,
                FileSize = fileSize,
                Version = newVersion
            });

            var module = _context.Modules.FirstOrDefault(m => m.Slug == moduleId);
            if (module != null)
            {
                module.LastFileUpload = DateTime.Now;
                module.Status = "Recently Updated";
                module.StatusColor = "updated";
                module.LastUpdated = DateTime.Now.ToString("MMM dd, yyyy");
            }

            _context.SaveChanges();
            return RedirectToAction("Module", new { id = moduleId });
        }



        [AllowAnonymous]
        // PREVIEW FILE — serves file directly or converts Office files to PDF
        public IActionResult PreviewFile(string moduleId, string fileName)
        {
            if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(fileName))
                return BadRequest("Invalid file.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId);
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var ext = Path.GetExtension(fileName).ToLower().TrimStart('.');

            // PDF — serve directly
            if (ext == "pdf")
                return File(System.IO.File.ReadAllBytes(filePath), "application/pdf");

            // Images — serve directly
            if (new[] { "png", "jpg", "jpeg", "gif", "webp" }.Contains(ext))
            {
                var mime = ext == "jpg" ? "image/jpeg" : $"image/{ext}";
                return File(System.IO.File.ReadAllBytes(filePath), mime);
            }

            // Word only — Aspose.Words can handle these
            if (new[] { "docx", "doc" }.Contains(ext))
            {
                var doc = new Aspose.Words.Document(filePath);
                var stream = new MemoryStream();
                doc.Save(stream, Aspose.Words.SaveFormat.Pdf);
                stream.Position = 0;
                return File(stream, "application/pdf");
            }

            // Excel and PowerPoint — not supported for preview, tell JS to show download
            return StatusCode(415);
        }



        [Authorize(Roles = "Admin, User")]
        [HttpPost]
        public IActionResult DeleteFile(int fileId, string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId))
                return BadRequest("Module ID is missing.");

            var file = _context.ModuleFiles.FirstOrDefault(f => f.Id == fileId);
            if (file != null)
            {
                file.IsDeleted = true;
                file.DeletedAt = DateTime.Now;
                file.DeletedBy = "Current User";
                _context.SaveChanges();
            }
            return RedirectToAction("Module", new { id = moduleId });
        }





        [Authorize(Roles = "Admin, User")]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult DownloadFile(string moduleId, string fileName)
        {
            if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(fileName))
                return BadRequest("Invalid file.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId);
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream"; // forces download
            return File(bytes, contentType, fileName);
        }






        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult AddModule(string title, string subtitle, string iconClass, string category, string categoryColor, string isFeatured)
        {
            if (string.IsNullOrEmpty(title))
                return RedirectToAction("WorkInstruction");

            var slug = title.ToLower()
                            .Replace(" ", "")
                            .Replace("/", "")
                            .Replace("-", "");

            if (_context.Modules.Any(m => m.Slug == slug))
                slug = slug + DateTime.Now.Ticks.ToString()[^4..];

            _context.Modules.Add(new Module
            {
                Slug = slug,
                Title = title,
                Subtitle = subtitle,
                IconClass = iconClass,
                Category = string.IsNullOrEmpty(category) ? "Uncategorized" : category,
                CategoryColor = string.IsNullOrEmpty(categoryColor) ? null : categoryColor,
                IsFeatured = isFeatured == "true",
                CreatedAt = DateTime.Now
            });
            
            _context.SaveChanges();
            return RedirectToAction("WorkInstruction");
        }





        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult EditModule(string slug, string title, string subtitle, string category, string iconClass, string categoryColor, string isFeatured)
        {
            var module = _context.Modules.FirstOrDefault(m => m.Slug == slug);
            if (module != null)
            {
                module.Title = title;
                module.Subtitle = subtitle;
                module.Category = category;
                module.IconClass = iconClass;
                module.CategoryColor = string.IsNullOrEmpty(categoryColor) ? null : categoryColor;
                module.IsFeatured = isFeatured == "true";
                _context.SaveChanges();
            }
            return RedirectToAction("WorkInstruction");
        }





        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteModule(string slug)
        {
            var module = _context.Modules.FirstOrDefault(m => m.Slug == slug);

            if (module != null)
            {
                // Delete all files in the folder
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", slug);
                if (Directory.Exists(uploadsFolder))
                    Directory.Delete(uploadsFolder, recursive: true);

                // Delete all file records from DB
                var files = _context.ModuleFiles.Where(f => f.ModuleSlug == slug).ToList();
                _context.ModuleFiles.RemoveRange(files);

                // Delete the module itself
                _context.Modules.Remove(module);
                _context.SaveChanges();
            }

            return RedirectToAction("WorkInstruction");
        }



        





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    
        public IActionResult Privacy()
            {
                return View();
            }


    }
}
