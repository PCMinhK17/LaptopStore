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
    }
}
