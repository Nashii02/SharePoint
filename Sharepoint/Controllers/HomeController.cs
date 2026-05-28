using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Models;

namespace Sharepoint.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return RedirectToAction("WorkInstruction");
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

        public IActionResult WorkInstruction()
        {
            var model = new WorkInstructionViewModel
            {
                FeaturedCards = new List<WorkInstructionCard>
                {
                    new() { Title="Asset Management System", Subtitle="Track IT assets across departments",
                            IconClass="ti ti-devices", Status="Updated",
                            StatusColor="updated", LastUpdated="Apr 28, 2026", Url="/ams" },
                    new() { Title="ONEHRMS", Subtitle="HR procedures for onboarding & payroll",
                            IconClass="ti ti-users", Status="New doc",
                            StatusColor="new", LastUpdated="Apr 25, 2026", Url="/hrms" },
                    new() { Title="ONEHRMS", Subtitle="HR procedures for onboarding & payroll",
                            IconClass="ti ti-users", Status="New doc",
                            StatusColor="new", LastUpdated="Apr 25, 2026", Url="/hrms" },
                },

                SecondaryCards = new List<WorkInstructionCard>
                {
                    new() { Title = "ONEHRMS", Subtitle = "HR procedures for onboarding & payroll", IconClass="", Url = "/Home/Module?id=hrms" },
                    new() { Title = "Budget", Subtitle = "Asset Management System", IconClass="fa-solid fa-dollar-sign", Url = "/Home/Module?id=bdgt" },
                    new() { Title = "Buying", Subtitle = "Asset Management System", IconClass="fa-solid fa-dollar-sign", Url = "/Home/Module?id=byng" },
                    new() { Title = "Rehandling", Subtitle = "Asset Management System", Url = "/Home/Module?id=rhndlng" },

                    new() { Title = "Asset Management", Subtitle = "HR procedures for onboarding & payroll", Url = "/Home/Module?id=hrms" },
                    new() { Title = "Uilis Administration", Subtitle = "Asset Management System", Url = "/Home/Module?id=bdgt" },
                    new() { Title = "ITPMS", Subtitle = "Asset Management System", Url = "/Home/Module?id=byng" },
                    new() { Title = "IRMS", Subtitle = "Asset Management System", Url = "/Home/Module?id=rhndlng" },

                    new() { Title = "EEMS", Subtitle = "HR procedures for onboarding & payroll", Url = "/Home/Module?id=eems" },
                    new() { Title = "SLIMS", Subtitle = "Asset Management System", Url = "/Home/Module?id=slms" },
                    new() { Title = "DPS", Subtitle = "Asset Management System", Url = "/Home/Module?id=dps" },
                    new() { Title = "Processing", Subtitle = "Asset Management System", Url = "/Home/Module?id=prcs" },

                    new() { Title = "Canteen Module", Subtitle = "HR procedures for onboarding & payroll", Url = "/Home/Module?id=cntmd" },
                    new() { Title = "Accomodation", Subtitle = "Asset Management System", Url = "/Home/Module?id=acmd" },
                    new() { Title = "Fleet Management", Subtitle = "Asset Management System", Url = "/Home/Module?id=flmg" },
                    new() { Title = "Flight Scheduling", Subtitle = "Asset Management System", Url = "/Home/Module?id=flsd" },

                    new() { Title = "IT Dev Ticketing", Subtitle = "HR procedures for onboarding & payroll", Url = "/Home/Module?id=itdt" },
                    new() { Title = "User Access Matrix", Subtitle = "Asset Management System", Url = "/Home/Module?id=uam" },
                    new() { Title = "IO Portal EHSS", Subtitle = "Asset Management System", Url = "/Home/Module?id=iope" },
                    new() { Title = "TRS / TCMS", Subtitle = "Asset Management System", Url = "/Home/Module?id=trsm" },
                    // In FeaturedCards or SecondaryCards:
new() { Title = "New Module", Subtitle = "Description here",
        IconClass = "fa-solid fa-box", Url = "/Home/Module?id=newmod" },
                }
            };

            return View(model);
        }

        public IActionResult Module(string id)
        {
            var titles = new Dictionary<string, (string Title, string Subtitle)>
            {
                // ... your titles ...
            };

            titles.TryGetValue(id ?? "", out var info);

            var model = new ModulePageViewModel
            {
                ModuleTitle = info.Title != null ? info.Title : "Module",
                ModuleSubtitle = info.Subtitle != null ? info.Subtitle : "Work instruction module",
                Files = new List<ModuleFile>() // keep this empty now
            };

            // ↓ ADD IT HERE, after model is built, before return
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", id ?? "");

            if (Directory.Exists(uploadsFolder))
            {
                var uploadedFiles = Directory.GetFiles(uploadsFolder).Select(f => new ModuleFile
                {
                    FileName = Path.GetFileNameWithoutExtension(f),
                    FileType = Path.GetExtension(f).TrimStart('.').ToLower(),
                    UploadedBy = "—",
                    UploadedDate = System.IO.File.GetCreationTime(f).ToString("MMM dd, yyyy"),
                    Url = $"/uploads/{id}/{Path.GetFileName(f)}"
                }).ToList();

                model.Files = uploadedFiles;
            }

            return View("Module", model); // ← always the last line
        }

        public IActionResult PreviewFile(string moduleId, string fileName)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId);
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var ext = Path.GetExtension(fileName).ToLower().TrimStart('.');

            // PDF and images — serve directly
            if (ext == "pdf")
            {
                var bytes = System.IO.File.ReadAllBytes(filePath);
                return File(bytes, "application/pdf");
            }

            if (new[] { "png", "jpg", "jpeg", "gif", "webp" }.Contains(ext))
            {
                var bytes = System.IO.File.ReadAllBytes(filePath);
                var mime = ext == "jpg" ? "image/jpeg" : $"image/{ext}";
                return File(bytes, mime);
            }

            // DOCX / XLSX / PPTX — convert to PDF first, then stream
            if (new[] { "docx", "doc", "xlsx", "xls", "pptx", "ppt" }.Contains(ext))
            {
                var doc = new Aspose.Words.Document(filePath);
                var stream = new MemoryStream();
                doc.Save(stream, Aspose.Words.SaveFormat.Pdf);
                stream.Position = 0;
                return File(stream, "application/pdf");
            }

            return BadRequest("Unsupported file type.");
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file, string moduleId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file selected.");

            // Define where to save — create folder per module
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", moduleId);
            Directory.CreateDirectory(uploadsFolder); // creates folder if it doesn't exist

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Redirect back to the same module page
            return RedirectToAction("Module", new { id = moduleId });
        }


    }
}
