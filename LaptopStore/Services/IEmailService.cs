namespace LaptopStore.Services
{
    /// <summary>
    /// Interface cho dịch vụ gửi email
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email xác thực tài khoản
        /// </summary>
        /// <param name="toEmail">Email người nhận</param>
        /// <param name="userName">Tên người dùng</param>
        /// <param name="verificationLink">Link xác thực</param>
        /// <returns>True nếu gửi thành công</returns>
        Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationLink);

        /// <summary>
        /// Gửi email thông báo xác thực thành công
        /// </summary>
        /// <param name="toEmail">Email người nhận</param>
        /// <param name="userName">Tên người dùng</param>
        /// <returns>True nếu gửi thành công</returns>
        Task<bool> SendVerificationSuccessEmailAsync(string toEmail, string userName);

        /// <summary>
        /// Gửi email thiết lập tài khoản (cho user do admin tạo)
        /// </summary>
        /// <param name="toEmail">Email người nhận</param>
        /// <param name="userName">Tên người dùng</param>
        /// <param name="setupLink">Link thiết lập mật khẩu</param>
        /// <returns>True nếu gửi thành công</returns>
        Task<bool> SendAccountSetupEmailAsync(string toEmail, string userName, string setupLink);

        /// <summary>
        /// Gửi email reset mật khẩu
        /// </summary>
        /// <param name="toEmail">Email người nhận</param>
        /// <param name="userName">Tên người dùng</param>
        /// <param name="resetLink">Link reset mật khẩu</param>
        /// <returns>True nếu gửi thành công</returns>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
    }
}
