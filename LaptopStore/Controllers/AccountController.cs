using System.Security.Claims;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using LaptopStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            LaptopStoreDbContext context,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        #region Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect nếu đã đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            // Clear TempData khi vào trang mới (tránh hiển thị toast từ lần trước)
            TempData.Clear();

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model.Input, model.Password, model.RememberMe);

            if (!result.Success)
            {
                // Chỉ hiển thị thông báo đơn giản, không có dấu *
                var errorMessage = result.ErrorMessage?.Contains("Tài khoản") == true 
                    ? "Tài khoản hoặc mật khẩu không chính xác"
                    : "Tài khoản hoặc mật khẩu không chính xác";
                
                ModelState.AddModelError(string.Empty, errorMessage);
                TempData["ToastMessage"] = errorMessage;
                TempData["ToastType"] = "error";
                return View(model);
            }

            if (result.User == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể xác thực người dùng.");
                return View(model);
            }

            // Đăng nhập thành công
            await SignInUserAsync(result.User, model.RememberMe);

            TempData["ToastMessage"] = $"Đăng nhập thành công! Chào mừng {result.User.FullName ?? result.User.Email}";
            TempData["ToastType"] = "success";

            // Redirect theo role hoặc returnUrl
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(
                result.RedirectAction ?? "Index",
                result.RedirectController ?? "Home");
        }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            // Redirect nếu đã đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Product");
            }

            // Clear TempData khi vào trang mới (tránh hiển thị toast từ lần trước)
            TempData.Clear();

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                // Thêm lỗi vào ModelState với field cụ thể nếu có thể
                if (result.ErrorMessage?.Contains("Email") == true)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Email), result.ErrorMessage);
                }
                else if (result.ErrorMessage?.Contains("Số điện thoại") == true)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.PhoneNumber), result.ErrorMessage);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại.");
                }

                TempData["ToastMessage"] = result.ErrorMessage ?? "Đăng ký thất bại.";
                TempData["ToastType"] = "error";
                return View(model);
            }

            if (result.User == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản.");
                return View(model);
            }

            // Đăng nhập tự động sau khi đăng ký
            await SignInUserAsync(result.User, false);

            TempData["ToastMessage"] = $"Đăng ký thành công! Chào mừng {result.User.FullName ?? result.User.Email}";
            TempData["ToastType"] = "success";

            return RedirectToAction(
                result.RedirectAction ?? "Index",
                result.RedirectController ?? "Product");
        }

        #endregion

        #region Validation APIs (for real-time validation)

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { exists = false, valid = false });
            }

            var normalized = email.Trim().ToLowerInvariant();

            // Validate email format
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            var isValid = emailRegex.IsMatch(normalized);

            if (!isValid)
            {
                return Json(new { exists = false, valid = false });
            }

            var exists = await _authService.CheckEmailExistsAsync(normalized);
            return Json(new { exists, valid = true });
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new { exists = false, valid = false });
            }

            var normalized = phoneNumber.Trim();

            // Validate phone format (9-11 digits)
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{9,11}$");
            var isValid = phoneRegex.IsMatch(normalized);

            if (!isValid)
            {
                return Json(new { exists = false, valid = false });
            }

            var exists = await _authService.CheckPhoneExistsAsync(normalized);
            return Json(new { exists, valid = true });
        }

        #endregion

        #region Google OAuth

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/Account/GoogleResponse"
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (result?.Principal == null)
                {
                    TempData["ToastMessage"] = "Không thể xác thực với Google. Vui lòng thử lại.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Login");
                }

                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ToastMessage"] = "Không thể lấy thông tin email từ Google.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Tạo user mới từ Google
                    var now = DateTime.Now;
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        Password = string.Empty, // Google OAuth không cần password
                        Role = "customer",
                        Status = "active",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New user registered via Google: {Email}", email);
                }

                // Đăng nhập
                await SignInUserAsync(user, false);

                TempData["ToastMessage"] = $"Đăng nhập bằng Google thành công! Chào mừng {user.FullName ?? user.Email}";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth");
                TempData["ToastMessage"] = "Có lỗi xảy ra khi đăng nhập bằng Google. Vui lòng thử lại.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công.";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Helper Methods

        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "customer"),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Product");
        }

        #endregion

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}
