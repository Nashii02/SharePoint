using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sharepoint.Data;
using Sharepoint.Models;
using Sharepoint.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Sharepoint.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                (u.Username == model.Username || u.Email == model.Username) && u.IsActive);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            await SignInUser(user, model.RememberMe);
            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("WorkInstruction", "Home");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "That username is already taken.");
                return View(model);
            }

            var user = new AppUser
            {
                Username = model.Username.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Username : model.DisplayName.Trim(),
                Email = model.Email?.Trim(),
                PasswordHash = HashPassword(model.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await SignInUser(user, isPersistent: false);
            TempData["SuccessMessage"] = $"Welcome, {user.DisplayName}!";
            return RedirectToAction("WorkInstruction", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                HasAvatar = user.AvatarData != null,
                UploadCount = await _db.WorkDocuments.CountAsync()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Avatar(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user?.AvatarData == null) return NotFound();
            return File(user.AvatarData, user.AvatarMimeType ?? "image/jpeg");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out int userId)) return RedirectToAction("Login");

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login");

            ViewBag.HasAvatar = user.AvatarData != null;
            ViewBag.UserId = user.Id;
            ViewBag.Username = user.Username;
            ViewBag.ProfileModel = new EditProfileViewModel { DisplayName = user.DisplayName, Email = user.Email };
            ViewBag.PasswordModel = new ChangePasswordViewModel();

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(EditProfileViewModel model)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out int userId)) return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login");

            user.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? user.Username : model.DisplayName.Trim();
            user.Email = model.Email?.Trim();

            if (model.RemoveAvatar)
            {
                user.AvatarData = null;
                user.AvatarMimeType = null;
                _db.Entry(user).Property(u => u.AvatarData).IsModified = true;
                _db.Entry(user).Property(u => u.AvatarMimeType).IsModified = true;
            }
            else if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowed.Contains(model.AvatarFile.ContentType.ToLower()))
                {
                    TempData["ErrorMessage"] = "Only JPG, PNG, GIF, or WEBP images are allowed.";
                    return RedirectToAction("Settings");
                }
                if (model.AvatarFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Avatar must be under 5 MB.";
                    return RedirectToAction("Settings");
                }

                using var ms = new MemoryStream();
                await model.AvatarFile.CopyToAsync(ms);
                user.AvatarData = ms.ToArray();
                user.AvatarMimeType = model.AvatarFile.ContentType;
            }

            await _db.SaveChangesAsync();
            await SignInUser(user, isPersistent: false);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Settings");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the errors and try again.";
                return RedirectToAction("Settings");
            }

            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Settings");
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Settings");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount()
        {
            var user = await GetCurrentUser();
            if (user == null) return RedirectToAction("Login");

            user.IsActive = false;
            await _db.SaveChangesAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task<AppUser?> GetCurrentUser()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out int id)) return null;
            return await _db.Users.FindAsync(id);
        }

        private async Task SignInUser(AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim("DisplayName",             user.DisplayName ?? user.Username),
                new Claim(ClaimTypes.Role,           user.Role),
            };
            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerifyPassword(string password, string storedHash)
            => HashPassword(password) == storedHash;

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("WorkInstruction", "Home");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"'{user.Username}' has been restored.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ToggleUserStatus(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            if (user.Role == "Admin") return Forbid(); 

            user.IsActive = !user.IsActive;
            _db.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _db.Users
                .OrderBy(u => u.Username)
                .ToListAsync();
            return View(users);
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (TempData["ResetEmailSent"] != null)
                ViewBag.ResetEmailSent = TempData["ResetEmailSent"];

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, [FromServices] EmailService emailService)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Please enter your email address.";
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                user.PasswordResetToken = Guid.NewGuid().ToString("N");
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _db.SaveChangesAsync();

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token = user.PasswordResetToken },
                    protocol: Request.Scheme);

                await emailService.SendPasswordResetAsync(email, resetLink!);
            }

            TempData["ResetEmailSent"] = email;
            return RedirectToAction("ForgotPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ForgotPassword");

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ForgotPassword");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters.";
                ViewBag.Token = token;
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                ViewBag.Token = token;
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                TempData["ErrorMessage"] = "This reset link is invalid or has expired.";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = HashPassword(password);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _db.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Your password has been reset. You can now sign in.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyAdminPassword([FromBody] VerifyPasswordRequest request)
        {
            var username = User.Identity?.Name;
            var admin = _db.Users.FirstOrDefault(u => u.Username == username);
            if (admin == null) return Unauthorized();

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToBase64String(
                sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(request.Password)));

            return hash == admin.PasswordHash ? Ok() : Unauthorized();
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteUser(int id, string adminPassword)
        {
            var username = User.Identity?.Name;
            var admin = _db.Users.FirstOrDefault(u => u.Username == username);
            if (admin == null) return Unauthorized();

            if (!VerifyPassword(adminPassword, admin.PasswordHash))
                return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            if (user.Role == "Admin") return Forbid();

            _db.Users.Remove(user);
            _db.SaveChanges();

            return RedirectToAction("ManageUsers");
        }

        public class VerifyPasswordRequest
        {
            public string Password { get; set; } = "";
        }

    }
}