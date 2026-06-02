using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AccountController(
            SignInManager<IdentityUser> signIn,
            UserManager<IdentityUser> users,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContext)  
        {
            _signIn = signIn;
            _users = users;
            _roleManager = roleManager;
            _httpContext = httpContext;
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
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("WorkInstruction", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                ViewData["ShowRegister"] = true;
                return View("Login", new LoginViewModel { RegisterModel = model });
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

            ViewData["ShowRegister"] = true;
            return View("Login", new LoginViewModel { RegisterModel = model });
        }

        // USER MANAGEMENT PAGE — Admin only
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var users = _users.Users.ToList();
            var allRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            var currentUserId = _users.GetUserId(User);

            var rows = new List<UserRow>();
            foreach (var user in users)
            {
                var roles = await _users.GetRolesAsync(user);
                rows.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            // Pin current admin to top, then sort rest by role then email
            var sorted = rows
                .OrderBy(u => u.Id == currentUserId ? 0 : 1)
                .ThenBy(u => u.Role switch
                {
                    "Admin" => 0,
                    "User" => 1,
                    _ => 2
                })
                .ThenBy(u => u.Email)
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
            var user = await _users.FindByIdAsync(userId);
            if (user != null)
                await _users.DeleteAsync(user);

            return RedirectToAction("ManageUsers");
        }

        public IActionResult AccessDenied() => View();
    }
}