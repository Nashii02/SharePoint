using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sharepoint.Data;
using Sharepoint.Models;
using Sharepoint.Services;
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
        private readonly EmailService _emailService;        // ← add this

        public AccountController(
            SignInManager<IdentityUser> signIn,
            UserManager<IdentityUser> users,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContext,
            AppDbContext context,
            EmailService emailService)
        {
            _signIn = signIn;
            _users = users;
            _roleManager = roleManager;
            _httpContext = httpContext;
            _context = context;
            _emailService = emailService;  
        }

        // ── LOGIN ──────────────────────────────────────────────
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
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("WorkInstruction", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked for 15 minutes due to too many failed attempts.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ── GUEST LOGIN ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> LoginAsGuest(GuestLoginViewModel model, string? returnUrl = null)
        {
            var sessionId = HttpContext.Session.Id ?? Guid.NewGuid().ToString();
            HttpContext.Session.SetString("GuestSessionId", sessionId);

            string baseNickname = string.IsNullOrWhiteSpace(model.Nickname)
                ? "Guest" : model.Nickname.Trim();

            var existingNicknames = _context.GuestUsers
                .Where(g => g.Nickname == baseNickname || g.Nickname.StartsWith(baseNickname + "#"))
                .Select(g => g.Nickname)
                .ToList();

            string nickname;
            if (!existingNicknames.Any())
                nickname = baseNickname;
            else
            {
                var number = 2;
                while (existingNicknames.Contains(baseNickname + "#" + number))
                    number++;
                nickname = baseNickname + "#" + number;
            }

            var guest = new GuestUser
            {
                Nickname = nickname,
                SessionId = sessionId,
                CreatedAt = DateTime.Now
            };

            _context.GuestUsers.Add(guest);
            await _context.SaveChangesAsync();

            // Sign in as a temporary identity user
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

        // ── LOGOUT ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            HttpContext.Session.Remove("GuestId");
            HttpContext.Session.Remove("GuestNickname");
            return RedirectToAction("Login");
        }

        // ── REGISTER ───────────────────────────────────────────
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("WorkInstruction", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // Check if email already taken
            var existing = await _users.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "An account with this email already exists.");
                return View(model);
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expires = DateTime.Now.AddMinutes(10).ToString("o");

            // Store registration data in TempData until verified
            TempData["Reg_Email"] = model.Email;
            TempData["Reg_Password"] = model.Password;
            TempData["Reg_OTP"] = otp;
            TempData["Reg_Expires"] = expires;
            TempData.Keep();

            // Send OTP email
            await _emailService.SendOtpAsync(model.Email, otp);

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            // If no pending registration, redirect back
            if (TempData["Reg_Email"] == null)
                return RedirectToAction("Register");

            TempData.Keep();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string code)
        {
            var email = TempData["Reg_Email"] as string;
            var password = TempData["Reg_Password"] as string;
            var otp = TempData["Reg_OTP"] as string;
            var expires = TempData["Reg_Expires"] as string;

            if (email == null || otp == null || expires == null)
            {
                TempData["Error"] = "Session expired. Please register again.";
                return RedirectToAction("Register");
            }

            // Check expiry
            if (DateTime.Parse(expires) < DateTime.Now)
            {
                TempData["Error"] = "Code has expired. Please register again.";
                return RedirectToAction("Register");
            }

            // Check code
            if (code.Trim() != otp)
            {
                TempData.Keep();
                TempData["OtpError"] = "Incorrect code. Please try again.";
                return View();
            }
            

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _users.CreateAsync(user, password!);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Register");
            }

            await _users.AddToRoleAsync(user, "User");

            TempData["Success"] = "Account created! You can now sign in.";
            return RedirectToAction("Login");
        }
        [HttpPost]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { success = false, message = "Email and password are required." });

            // Check if email already taken
            var existing = await _users.FindByEmailAsync(request.Email);
            if (existing != null)
                return Ok(new { success = false, message = "An account with this email already exists." });

            // Generate OTP and store in TempData
            var otp = new Random().Next(100000, 999999).ToString();
            var expires = DateTime.Now.AddMinutes(10).ToString("o");

            TempData["Reg_Email"] = request.Email;
            TempData["Reg_Password"] = request.Password;
            TempData["Reg_OTP"] = otp;
            TempData["Reg_Expires"] = expires;
            TempData.Keep();

            await _emailService.SendOtpAsync(request.Email, otp);

            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string code, string email, string password)
        {
            var otpStored = TempData["Reg_OTP"] as string;
            var expires = TempData["Reg_Expires"] as string;

            // Fallback to hidden fields if TempData expired
            var regEmail = (TempData["Reg_Email"] as string) ?? email;
            var regPassword = (TempData["Reg_Password"] as string) ?? password;

            if (string.IsNullOrEmpty(otpStored) || string.IsNullOrEmpty(expires))
            {
                TempData["Error"] = "Session expired. Please register again.";
                return RedirectToAction("Register");
            }

            if (DateTime.Parse(expires) < DateTime.Now)
            {
                TempData["Error"] = "Code has expired. Please register again.";
                return RedirectToAction("Register");
            }

            if (code.Trim() != otpStored)
            {
                TempData.Keep();
                TempData["OtpError"] = "Incorrect code. Please try again.";
                return RedirectToAction("Register");
            }

            var user = new IdentityUser
            {
                UserName = regEmail,
                Email = regEmail,
                EmailConfirmed = true
            };

            var result = await _users.CreateAsync(user, regPassword!);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Register");
            }

            await _users.AddToRoleAsync(user, "User");
            TempData["Success"] = "Account created! You can now sign in.";
            return RedirectToAction("Login");
        }

        // Resend OTP
        [HttpPost]
        public async Task<IActionResult> ResendOtp()
        {
            var email = TempData["Reg_Email"] as string;
            if (email == null) return RedirectToAction("Register");

            var otp = new Random().Next(100000, 999999).ToString();
            var expires = DateTime.Now.AddMinutes(10).ToString("o");

            TempData["Reg_OTP"] = otp;
            TempData["Reg_Expires"] = expires;
            TempData.Keep();

            await _emailService.SendOtpAsync(email, otp);

            TempData["OtpInfo"] = "A new code has been sent to your email.";
            return RedirectToAction("VerifyOtp");
        }

        // ── MANAGE USERS ───────────────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var identityUsers = _users.Users.ToList();
            var guestUsers = _context.GuestUsers.ToList();
            var allRoles = new List<string> { "Admin", "User", "Guest" };
            var currentUserId = _users.GetUserId(User);

            var rows = new List<UserRow>();

            foreach (var user in identityUsers)
            {
                var roles = await _users.GetRolesAsync(user);
                rows.Add(new UserRow
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    DisplayName = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsGuest = false,
                    IsVerified = user.EmailConfirmed
                });
            }

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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (userId.StartsWith("guest-"))
                return RedirectToAction("ManageUsers");

            var user = await _users.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("ManageUsers");

            var currentRoles = await _users.GetRolesAsync(user);
            await _users.RemoveFromRolesAsync(user, currentRoles);
            await _users.AddToRoleAsync(user, newRole);
            return RedirectToAction("ManageUsers");
        }

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
                    var reactions = _context.ModuleReactions
                        .Where(r => r.UserId == userId).ToList();
                    _context.ModuleReactions.RemoveRange(reactions);

                    var comments = _context.ModuleComments
                        .Where(c => c.UserId == userId).ToList();
                    foreach (var c in comments)
                    {
                        c.UserId = "deleted";
                        c.UserEmail = "[deleted]";
                    }

                    _context.GuestUsers.Remove(guest);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var user = await _users.FindByIdAsync(userId);
                if (user != null)
                {
                    var reactions = _context.ModuleReactions
                        .Where(r => r.UserId == userId).ToList();
                    _context.ModuleReactions.RemoveRange(reactions);

                    await _users.DeleteAsync(user);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("ManageUsers");
        }

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