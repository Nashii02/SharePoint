using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Data;
using Sharepoint.Models;
using System.ComponentModel.DataAnnotations;

namespace Sharepoint.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly UserManager<IdentityUser> _users;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContext;
        private readonly AppDbContext _context;

        public AccountController(
            SignInManager<IdentityUser> signIn,
            UserManager<IdentityUser> users,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContext,
            AppDbContext context)
        {
            _signIn = signIn;
            _users = users;
            _roleManager = roleManager;
            _httpContext = httpContext;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("WorkInstruction", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("WorkInstruction", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsGuest(GuestLoginViewModel model, string? returnUrl = null)
        {
            var sessionId = HttpContext.Session.Id ?? Guid.NewGuid().ToString();
            HttpContext.Session.SetString("GuestSessionId", sessionId);

            // Auto-generate nickname if not provided
            string nickname = string.IsNullOrWhiteSpace(model.Nickname)
                ? $"Guest{GenerateGuestNumber()}"
                : model.Nickname.Trim();

            var guest = new GuestUser
            {
                Nickname = nickname,
                SessionId = sessionId,
                CreatedAt = DateTime.Now
            };

            _context.GuestUsers.Add(guest);
            await _context.SaveChangesAsync();

            // Create a synthetic claim for guest user
            await _signIn.SignInAsync(new IdentityUser
            {
                Id = $"guest-{guest.Id}",
                Email = nickname,
                UserName = nickname
            }, isPersistent: false);

            HttpContext.Session.SetString("GuestId", guest.Id.ToString());
            HttpContext.Session.SetString("GuestNickname", nickname);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("WorkInstruction", "Home");
        }

        private int GenerateGuestNumber()
        {
            var lastGuest = _context.GuestUsers
                .OrderByDescending(g => g.Id)
                .FirstOrDefault();
            return lastGuest?.Id + 1 ?? 1;
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            HttpContext.Session.Remove("GuestId");
            HttpContext.Session.Remove("GuestNickname");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Login", "Account");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                NormalizedEmail = model.Email.ToUpper(),
                NormalizedUserName = model.Email.ToUpper()
            };

            var result = await _users.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(user, "User");
                await _signIn.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("WorkInstruction", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // USER MANAGEMENT PAGE — Admin only
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var identityUsers = _users.Users.ToList();
            var guestUsers = _context.GuestUsers.ToList();
            var allRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            var currentUserId = _users.GetUserId(User);

            var rows = new List<UserRow>();

            // Add registered users
            foreach (var user in identityUsers)
            {
                var roles = await _users.GetRolesAsync(user);
                rows.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    DisplayName = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsGuest = false
                });
            }

            // Add guest users
            foreach (var guest in guestUsers)
            {
                rows.Add(new UserRow
                {
                    Id = $"guest-{guest.Id}",
                    Email = guest.Nickname,
                    DisplayName = guest.Nickname,
                    Role = "Guest",
                    IsGuest = true,
                    IsBanned = guest.IsBanned
                });
            }

            // Sort: current admin first, then by role, then by name
            var sorted = rows
                .OrderBy(u => u.Id == currentUserId ? 0 : 1)
                .ThenBy(u => u.Role switch
                {
                    "Admin" => 0,
                    "User" => 1,
                    "Guest" => 2,
                    _ => 3
                })
                .ThenBy(u => u.DisplayName)
                .ToList();

            return View(new UserManagementViewModel
            {
                Users = sorted,
                AllRoles = allRoles
            });
        }

        // CHANGE ROLE — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (userId.StartsWith("guest-"))
            {
                // Cannot change guest role — they're always "Guest"
                return RedirectToAction("ManageUsers");
            }

            var user = await _users.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("ManageUsers");

            var currentRoles = await _users.GetRolesAsync(user);
            await _users.RemoveFromRolesAsync(user, currentRoles);
            await _users.AddToRoleAsync(user, newRole);

            return RedirectToAction("ManageUsers");
        }

        // DELETE USER — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (userId.StartsWith("guest-"))
            {
                var guestId = int.Parse(userId.Replace("guest-", ""));
                var guest = _context.GuestUsers.FirstOrDefault(g => g.Id == guestId);
                if (guest != null)
                {
                    _context.GuestUsers.Remove(guest);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var user = await _users.FindByIdAsync(userId);
                if (user != null)
                    await _users.DeleteAsync(user);
            }

            return RedirectToAction("ManageUsers");
        }

        // BAN GUEST — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> BanGuest(int guestId, string? reason = null)
        {
            var guest = _context.GuestUsers.FirstOrDefault(g => g.Id == guestId);
            if (guest != null)
            {
                guest.IsBanned = true;
                guest.BannedAt = DateTime.Now;
                guest.BannedReason = reason;
                _context.GuestUsers.Update(guest);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageUsers");
        }

        public IActionResult AccessDenied() => View();
    }
}
