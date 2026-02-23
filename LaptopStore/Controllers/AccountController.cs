using System.Security.Claims;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LaptopStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(LaptopStoreDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== LOGIN ==========

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var identifier = model.Input?.Trim();
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng nhập Email hoặc Số điện thoại.");
                    return View(model);
                }

                User? user;

                if (identifier.Contains("@"))
                {
                    user = await _context.Users.SingleOrDefaultAsync(u => u.Email == identifier);
                }
                else
                {
                    user = await _context.Users.SingleOrDefaultAsync(u => u.PhoneNumber == identifier);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                    TempData["ToastMessage"] = "Tài khoản không tồn tại.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                bool isPasswordValid = false;
                try
                {
                    isPasswordValid = BCryptNet.Verify(model.Password, user.Password);
                }
                catch
                {
                    if (user.Password == model.Password) isPasswordValid = true;
                }

                if (!isPasswordValid)
                {
                    ModelState.AddModelError(string.Empty, "Mật khẩu không chính xác.");
                    TempData["ToastMessage"] = "Mật khẩu không chính xác.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
                    TempData["ToastMessage"] = "Tài khoản đã bị khóa.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                await SignInUser(user, model.RememberMe);

                TempData["ToastMessage"] = $"Đăng nhập thành công! Chào {user.FullName ?? user.Email}";
                TempData["ToastType"] = "success";

                switch (user.Role)
                {
                    case "admin":
                        return RedirectToAction("Index", "ProductManagement");
                    case "staff":
                        return RedirectToAction("Index", "Home");
                    case "customer":
                        return RedirectToAction("Index", "Product");
                    default:
                        return RedirectToAction("Index", "Home");
                }  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập");

                TempData["ToastMessage"] = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại.";
                TempData["ToastType"] = "error";

                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống.");
                return View(model);
            }
        }

        // ========== REGISTER ==========

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Product");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var email = model.Email?.Trim() ?? string.Empty;
                var phone = model.PhoneNumber?.Trim() ?? string.Empty;

                if (await _context.Users.AnyAsync(u => u.Email == email))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email này đã được sử dụng.");
                    TempData["ToastMessage"] = "Email đã tồn tại.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.PhoneNumber == phone))
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.PhoneNumber), "Số điện thoại này đã được sử dụng.");
                    TempData["ToastMessage"] = "Số điện thoại đã tồn tại.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                var now = DateTime.Now;

                var user = new User
                {
                    FullName = model.FullName,
                    Email = email,
                    PhoneNumber = phone,
                    Address = model.Address,
                    Password = BCryptNet.HashPassword(model.Password),
                    Role = "customer",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SignInUser(user, false);

                TempData["ToastMessage"] = "Đăng ký và đăng nhập thành công!";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký");

                TempData["ToastMessage"] = "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại.";
                TempData["ToastType"] = "error";

                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { exists = false });

            var normalized = email.Trim();
            var exists = await _context.Users.AnyAsync(u => u.Email == normalized);
            return Json(new { exists });
        }

        [HttpGet]
        public async Task<IActionResult> CheckPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Json(new { exists = false });

            var normalized = phoneNumber.Trim();
            var exists = await _context.Users.AnyAsync(u => u.PhoneNumber == normalized);
            return Json(new { exists });
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

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Bạn đã đăng xuất.";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (result.Principal == null)
            {
                TempData["ToastMessage"] = "Không xác thực được với Google.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (email != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    var now = DateTime.Now;
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        Password = string.Empty,
                        Role = "customer",
                        Status = "active",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                await SignInUser(user, false);

                TempData["ToastMessage"] = "Đăng nhập bằng Google thành công!";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "Home");
            }

            TempData["ToastMessage"] = "Không lấy được thông tin Google.";
            TempData["ToastType"] = "error";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}
