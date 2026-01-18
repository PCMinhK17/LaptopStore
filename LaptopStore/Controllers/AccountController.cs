using System.Security.Claims;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public AccountController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find user by Email or Phone
                // Note: Existing passwords in DB are simple strings or placeholders (e.g. hash_admin_123).
                // For this implementation, we will do a direct comparison or simple check.
                // In production, USE PROPER HASHING (BCrypt/Argon2).
                
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => (u.Email == model.Input || u.PhoneNumber == model.Input));

                if (user != null)
                {
                    // WARNING: Simple comparison for demonstration/existing seed data compatibility.
                    // Replace with PasswordHasher verification in production.
                    bool isPasswordValid = user.Password == model.Password; 

                    if (isPasswordValid)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role ?? "customer"),
                            new Claim("UserId", user.Id.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        };

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                        return RedirectToAction("Index", "Home");
                    }
                }
                
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            // If using the default flow where Google signs into a separate cookie, reading it here might need 'External' scheme.
            // But if we just Challenge GoogleDefaults, it will try to sign in.
            // Actually, usually we configure AddGoogle to signin to an 'External' cookie scheme first because the main scheme is Application Cookie.
            // However, with minimal config, `AuthenticateAsync(GoogleDefaults.AuthenticationScheme)` might not return the user if the middleware handled it and signed in the default scheme?
            // Wait, AddAuthentication().AddCookie() sets the default scheme to Cookie.
            // Challenge(Google) -> Redirects to Google -> Redirects back to /signin-google.
            // The GoogleHandler handles the callback. It constructs a ticket. It calls `SignInAsync` on the `SignInScheme`.
            // By default `SignInScheme` falls back to `DefaultSignInScheme` which is Cookie.
            // So the user MIGHT be already signed in after the callback?
            // BUT, usually we want to intercept to link users.
            
            // Standard flow:
            // 1. User is signed in via Cookie (as a result of Google Handler).
            // 2. We verify if the email exists in our DB.
            
            // Let's check if User is authenticated.
            if (result.Principal == null && !User.Identity!.IsAuthenticated)
            {
               // Failed
               return RedirectToAction("Login");
            }

            // Get claims from the authenticated user (either from result or User)
            var claimsPrincipal = result.Principal ?? User;
            var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            var name = claimsPrincipal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Create new user if not exists (Optional feature, let's just create for convenience)
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        Password = "", // No password for Google users
                        Role = "customer",
                        Status = "active",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Sign in with our Application Claims (if different or enriched)
                // If Google already signed in, we are good, but we might want to ensure roles are present.
                // Re-issuing the cookie with our DB roles:
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName ?? email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "customer"),
                    new Claim("UserId", user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}
