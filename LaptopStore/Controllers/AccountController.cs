using System.Security.Claims;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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
            return View(new AuthViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login([Bind(Prefix = "Login")] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", new AuthViewModel { Login = model, ActiveTab = "login" });
            }

            // Tìm user theo Email hoặc Số điện thoại (tùy bạn đang cho nhập gì trong Input)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Input || u.PhoneNumber == model.Input);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return View("Login", new AuthViewModel { Login = model, ActiveTab = "login" });
            }

            bool isPasswordValid = false;
            try
            {
                // Nếu trong DB đang lưu password dạng hash BCrypt
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
            }
            catch
            {
                // Trường hợp password trong DB là plain text (cho data cũ)
                if (user.Password == model.Password)
                    isPasswordValid = true;
            }

            if (!isPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác.");
                return View("Login", new AuthViewModel { Login = model, ActiveTab = "login" });
            }

            if (user.Status != "active")
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
                return View("Login", new AuthViewModel { Login = model, ActiveTab = "login" });
            }

            await SignInUser(user, model.RememberMe);
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return Json(new { exists = false });
            
            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            return Json(new { exists = exists });
        }

        [HttpPost]
        public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Register.Email", "Email này đã được sử dụng.");
                    return View("Login", new AuthViewModel { Register = model, ActiveTab = "register" });
                }

                 if (await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
                {
                    ModelState.AddModelError("Register.PhoneNumber", "Số điện thoại này đã được sử dụng.");
                    return View("Login", new AuthViewModel { Register = model, ActiveTab = "register" });
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Password = global::BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "customer",
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SignInUser(user, false);
                return RedirectToAction("Index", "Home");
            }

            return View("Login", new AuthViewModel { Register = model, ActiveTab = "register" });
        }

        private async Task SignInUser(User user, bool isPersistent)
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
                IsPersistent = isPersistent
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
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
            if (result.Principal == null) return RedirectToAction("Login");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        Password = "",
                        Role = "customer",
                        Status = "active",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                await SignInUser(user, false);
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
