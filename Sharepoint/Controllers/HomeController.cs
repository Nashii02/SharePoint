using Aspose.Words;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Data;
using Sharepoint.Models;
using System.Diagnostics;


namespace Sharepoint.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
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


        // MODULE PAGE — shows files for a specific module
        // VIEW COUNT — increment on every module page load including guests
        [AllowAnonymous]
        public IActionResult Module(string id)
        {
            var module = _context.Modules.FirstOrDefault(m => m.Slug == id);
            if (module != null)
            {
                module.ViewCount++;
                _context.SaveChanges();
            }

            var currentUserId = _userManager.GetUserId(User);

            var model = new ModulePageViewModel
            {
                ModuleTitle = module?.Title ?? "Module",
                ModuleSubtitle = module?.Subtitle ?? "",
                ModuleSlug = id ?? "",
                ViewCount = module?.ViewCount ?? 0,
                LikeCount = _context.ModuleReactions.Count(r => r.ModuleSlug == id),
                IsLikedByMe = currentUserId != null &&
                                 _context.ModuleReactions.Any(r => r.ModuleSlug == id && r.UserId == currentUserId),
                Comments = _context.ModuleComments
                                    .Where(c => c.ModuleSlug == id && !c.IsDeleted)
                                    .OrderByDescending(c => c.CreatedAt)
                                    .ToList(),
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

        // LIKE / UNLIKE TOGGLE
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ToggleLike(string moduleId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var existing = _context.ModuleReactions
                .FirstOrDefault(r => r.ModuleSlug == moduleId && r.UserId == userId);

            if (existing != null)
                _context.ModuleReactions.Remove(existing);
            else
                _context.ModuleReactions.Add(new ModuleReaction
                {
                    ModuleSlug = moduleId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                });

            _context.SaveChanges();

            var count = _context.ModuleReactions.Count(r => r.ModuleSlug == moduleId);
            return Json(new { liked = existing == null, count });
        }

        // ADD COMMENT
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AddComment(string moduleId, string content)
        {
            string userId;
            string displayName;

            if (User.Identity?.IsAuthenticated == true)
            {
                var guestId = HttpContext.Session.GetString("GuestId");
                var guestNickname = HttpContext.Session.GetString("GuestNickname");

                if (guestId != null && guestNickname != null)
                {
                    // Guest posting a comment
                    userId = $"guest-{guestId}";
                    displayName = guestNickname;
                }
                else
                {
                    // Registered user
                    userId = _userManager.GetUserId(User)!;
                    displayName = User.Identity.Name ?? userId;
                }
            }
            else
            {
                return Forbid();
            }

            var comment = new ModuleComment
            {
                ModuleSlug = moduleId,
                Content = content,
                UserId = userId,
                UserEmail = displayName,
                CreatedAt = DateTime.Now
            };

            _context.ModuleComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Module", new { id = moduleId });
        }



        // EDIT COMMENT — owner or admin
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditComment(int commentId, string moduleId, string content)
        {
            var comment = await _context.ModuleComments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var guestId = HttpContext.Session.GetString("GuestId");
            var isOwner = comment.UserId == currentUserId ||
                          (guestId != null && comment.UserId == $"guest-{guestId}");

            if (!isOwner) return Forbid();

            comment.Content = content;
            comment.EditedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction("Module", new { id = moduleId });
        }

        // DELETE COMMENT — admin or comment owner
        [Authorize]
        [HttpPost]
        public IActionResult DeleteComment(int commentId, string moduleId)
        {
            var userId = _userManager.GetUserId(User);
            var guestId = HttpContext.Session.GetString("GuestId");
            var comment = _context.ModuleComments.FirstOrDefault(c => c.Id == commentId);

            if (comment != null)
            {
                bool isOwner = comment.UserId == userId ||
                              (guestId != null && comment.UserId == $"guest-{guestId}");
                
                if (User.IsInRole("Admin") || isOwner)
                {
                    comment.IsDeleted = true;
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Module", new { id = moduleId });
        }




        [Authorize(Roles = "Admin")]
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



        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteFile(int fileId, string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId))
                return BadRequest("Module ID is missing.");

            var file = _context.ModuleFiles.FirstOrDefault(f => f.Id == fileId);
            if (file != null)
            {
                // Delete physical file from disk
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId, file.FileName + "." + file.FileType);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                // Remove record from DB entirely
                _context.ModuleFiles.Remove(file);
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

            // Auto create a folder for the module
            var moduleFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", slug);
            Directory.CreateDirectory(moduleFolder);

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
