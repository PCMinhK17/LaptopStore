using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LaptopStore.Services
{
    public class AuthService : IAuthService
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(LaptopStoreDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string identifier, string password, bool rememberMe)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Vui lòng nhập đầy đủ thông tin."
                    };
                }

                var user = await GetUserByIdentifierAsync(identifier.Trim());

                // Security: Không phân biệt user không tồn tại hay password sai
                bool isPasswordValid = false;
                if (user != null)
                {
                    isPasswordValid = await VerifyPasswordAsync(user, password);
                }

                if (user == null || !isPasswordValid)
                {
                    _logger.LogWarning("Login failed for identifier: {Identifier}", identifier);
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác"
                    };
                }

                // Kiểm tra trạng thái tài khoản
                if (string.Equals(user.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    return new AuthResult
                    {
                        Success = false,
                        User = user,
                        RequiresEmailVerification = true,
                        ErrorMessage = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email của bạn."
                    };
                }

                if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hỗ trợ."
                    };
                }

                // Xác định redirect dựa trên role
                var (controller, action) = GetRedirectByRole(user.Role);

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    RedirectController = controller,
                    RedirectAction = action
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for identifier: {Identifier}", identifier);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<AuthResult> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var email = model.Email?.Trim() ?? string.Empty;
                var phone = model.PhoneNumber?.Trim() ?? string.Empty;

                // Kiểm tra email đã tồn tại
                if (await CheckEmailExistsAsync(email))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Email này đã được sử dụng. Vui lòng sử dụng email khác."
                    };
                }

                // Kiểm tra số điện thoại đã tồn tại
                if (await CheckPhoneExistsAsync(phone))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Số điện thoại này đã được sử dụng. Vui lòng sử dụng số điện thoại khác."
                    };
                }

                var now = DateTime.Now;

                var user = new User
                {
                    FullName = model.FullName?.Trim(),
                    Email = email,
                    PhoneNumber = phone,
                    Address = model.Address?.Trim(),
                    Password = BCryptNet.HashPassword(model.Password),
                    Role = "customer",
                    Status = "pending", // Đổi từ "active" thành "pending" để chờ xác thực email
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered (pending verification): {Email}", email);

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    RequiresEmailVerification = true, // Đánh dấu cần xác thực email
                    RedirectController = "Account",
                    RedirectAction = "VerificationSent"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _context.Users.AnyAsync(u => u.Email == email.Trim());
        }

        public async Task<bool> CheckPhoneExistsAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber.Trim());
        }

        public async Task<User?> GetUserByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            var normalized = identifier.Trim();

            if (normalized.Contains("@"))
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == normalized);
            }
            else
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalized);
            }
        }

        public Task<bool> VerifyPasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            try
            {
                // Thử verify với BCrypt
                var isValid = BCryptNet.Verify(password, user.Password);
                return Task.FromResult(isValid);
            }
            catch
            {
                // Fallback cho password cũ (plain text - không nên dùng trong production)
                if (user.Password == password)
                    return Task.FromResult(true);

                return Task.FromResult(false);
            }
        }

        private (string controller, string action) GetRedirectByRole(string? role)
        {
            return role?.ToLower() switch
            {
                "admin" => ("ProductManagement", "Index"),
                "staff" => ("Home", "Index"),
                "customer" => ("Product", "Index"),
                _ => ("Home", "Index")
            };
        }

        #region Email Verification Methods

        public async Task<string> GenerateEmailVerificationTokenAsync(int userId)
        {
            try
            {
                // Vô hiệu hóa các token cũ
                var oldTokens = await _context.EmailVerificationTokens
                    .Where(t => t.UserId == userId && !t.IsUsed)
                    .ToListAsync();

                foreach (var oldToken in oldTokens)
                {
                    oldToken.IsUsed = true;
                }

                // Tạo token mới
                var token = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;

                var verificationToken = new EmailVerificationToken
                {
                    UserId = userId,
                    Token = token,
                    CreatedAt = now,
                    ExpiresAt = now.AddHours(24), // Hết hạn sau 24 giờ
                    IsUsed = false
                };

                _context.EmailVerificationTokens.Add(verificationToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated email verification token for user {UserId}", userId);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email verification token for user {UserId}", userId);
                throw;
            }
        }

        public async Task<EmailVerificationResult> VerifyEmailTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Token không hợp lệ."
                    };
                }

                var verificationToken = await _context.EmailVerificationTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token);

                if (verificationToken == null)
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Token không tồn tại hoặc đã hết hạn."
                    };
                }

                if (verificationToken.IsUsed)
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Token này đã được sử dụng."
                    };
                }

                if (verificationToken.ExpiresAt < DateTime.Now)
                {
                    return new EmailVerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Token đã hết hạn. Vui lòng yêu cầu gửi lại email xác thực."
                    };
                }

                // Đánh dấu token đã sử dụng
                verificationToken.IsUsed = true;

                // Cập nhật trạng thái user
                var user = verificationToken.User;
                if (user != null)
                {
                    user.Status = "active";
                    user.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Email verified successfully for user {UserId}", verificationToken.UserId);

                return new EmailVerificationResult
                {
                    Success = true,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email token");
                return new EmailVerificationResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi khi xác thực email. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<bool> ResendVerificationEmailAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Resend verification email failed: user {UserId} not found", userId);
                    return false;
                }

                if (user.Status == "active")
                {
                    _logger.LogWarning("Resend verification email failed: user {UserId} already verified", userId);
                    return false;
                }

                // Token sẽ được tạo trong controller và gửi email
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email for user {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        #endregion
    }
}
