using LaptopStore.Models;
using LaptopStore.Models.ViewModels;

namespace LaptopStore.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string identifier, string password, bool rememberMe);
        Task<AuthResult> RegisterAsync(RegisterViewModel model);
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> CheckPhoneExistsAsync(string phoneNumber);
        Task<User?> GetUserByIdentifierAsync(string identifier);
        Task<bool> VerifyPasswordAsync(User user, string password);
        
        // Email Verification Methods
        Task<string> GenerateEmailVerificationTokenAsync(int userId);
        Task<EmailVerificationResult> VerifyEmailTokenAsync(string token);
        Task<bool> ResendVerificationEmailAsync(int userId);
        Task<User?> GetUserByIdAsync(int userId);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        
        /// <summary>
        /// Cho biết user cần xác thực email
        /// </summary>
        public bool RequiresEmailVerification { get; set; }
    }

    public class EmailVerificationResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
