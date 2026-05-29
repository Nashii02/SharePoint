using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sharepoint.Data;
using Sharepoint.Models;
using System.Text.RegularExpressions;

namespace Sharepoint.Controllers
{
    public class WorkInstructionsController : Controller
    {
        private readonly AppDbContext _db;

        public WorkInstructionsController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Upload(string category)
        {
            var cat = await _db.WorkCategories.FirstOrDefaultAsync(c => c.Slug == category);
            if (cat == null) return NotFound();

            ViewBag.Category = cat.Slug;
            ViewBag.CategoryName = cat.Name;
            ViewBag.Documents = await _db.WorkDocuments
                .Where(d => d.Category == category)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(string category, string documentTitle,
                                                     string documentDescription, IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a PDF file.";
                return RedirectToAction("Upload", new { category });
            }

            if (Path.GetExtension(pdfFile.FileName).ToLower() != ".pdf")
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed.";
                return RedirectToAction("Upload", new { category });
            }

            using var memoryStream = new MemoryStream();
            await pdfFile.CopyToAsync(memoryStream);

            var document = new WorkDocument
            {
                Title = documentTitle,
                Description = documentDescription,
                Category = category,
                FileName = pdfFile.FileName,
                FileData = memoryStream.ToArray(),
                FileSizeBytes = pdfFile.Length,
                UploadedAt = DateTime.Now
            };

            _db.WorkDocuments.Add(document);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{documentTitle}' uploaded successfully!";
            return RedirectToAction("Upload", new { category });
        }

        public async Task<IActionResult> Download(int id, bool inline = false)
        {
            var doc = await _db.WorkDocuments.FindAsync(id);
            if (doc == null) return NotFound();

            var contentDisposition = inline
                ? $"inline; filename=\"{doc.FileName}\""
                : $"attachment; filename=\"{doc.FileName}\"";

            Response.Headers["Content-Disposition"] = contentDisposition;
            return File(doc.FileData, "application/pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string category)
        {
            var doc = await _db.WorkDocuments.FindAsync(id);
            if (doc != null)
            {
                _db.WorkDocuments.Remove(doc);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Document deleted successfully.";
            }
            return RedirectToAction("Upload", new { category });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string description, string icon)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToAction("WorkInstruction", "Home");
            }

            var slug = Regex.Replace(name.ToLower().Trim(), @"[^a-z0-9]+", "-").Trim('-');

            if (await _db.WorkCategories.AnyAsync(c => c.Slug == slug))
            {
                TempData["ErrorMessage"] = "A category with that name already exists.";
                return RedirectToAction("WorkInstruction", "Home");
            }

            _db.WorkCategories.Add(new WorkCategory
            {
                Slug = slug,
                Name = name.Trim(),
                Description = description?.Trim(),
                Icon = icon ?? "folder",
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{name}' category created.";
            return RedirectToAction("Upload", new { category = slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.WorkCategories.FindAsync(id);
            if (cat == null || cat.IsDefault) return NotFound();

            var docs = _db.WorkDocuments.Where(d => d.Category == cat.Slug);
            _db.WorkDocuments.RemoveRange(docs);
            _db.WorkCategories.Remove(cat);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{cat.Name}' category deleted.";
            return RedirectToAction("WorkInstruction", "Home");
        }
    }
}