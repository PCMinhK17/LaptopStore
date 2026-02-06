using System.Net;
using System.Net.Mail;

namespace LaptopStore.Services
{
    /// <summary>
    /// Dịch vụ gửi email sử dụng SMTP
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationLink)
        {
            try
            {
                var subject = "Xác thực tài khoản LaptopStore";
                var body = GenerateVerificationEmailBody(userName, verificationLink);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email xác thực đến {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendVerificationSuccessEmailAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "Xác thực tài khoản thành công - LaptopStore";
                var body = GenerateVerificationSuccessEmailBody(userName);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email thông báo xác thực thành công đến {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendAccountSetupEmailAsync(string toEmail, string userName, string setupLink)
        {
            try
            {
                var subject = "Chào mừng đến với LaptopStore - Thiết lập tài khoản";
                var body = GenerateAccountSetupEmailBody(userName, setupLink);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email thiết lập tài khoản đến {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
                var smtpUser = smtpSettings["SmtpUser"] ?? "";
                var smtpPassword = smtpSettings["SmtpPassword"] ?? "";
                var senderEmail = smtpSettings["SenderEmail"] ?? smtpUser;
                var senderName = smtpSettings["SenderName"] ?? "LaptopStore";

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("Cấu hình SMTP chưa được thiết lập. Bỏ qua việc gửi email.");
                    // Trả về true để không block flow đăng ký khi chưa cấu hình email
                    return true;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Đã gửi email thành công đến {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email đến {Email}", toEmail);
                return false;
            }
        }

        private static string GenerateVerificationEmailBody(string userName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>💻 LaptopStore</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px;'>Xác thực tài khoản của bạn</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 22px;'>Xin chào {userName}! 👋</h2>
            
            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                Cảm ơn bạn đã đăng ký tài khoản tại LaptopStore. Để hoàn tất quá trình đăng ký và bảo mật tài khoản, vui lòng xác thực email của bạn.
            </p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <a href='{verificationLink}' 
                   style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                          color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; 
                          font-size: 16px; font-weight: bold; box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);'>
                    Xác thực Email
                </a>
            </div>
            
            <p style='color: #888888; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                Hoặc copy và dán link sau vào trình duyệt:
            </p>
            <p style='color: #667eea; font-size: 13px; word-break: break-all; background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 0 0 25px 0;'>
                {verificationLink}
            </p>
            
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 25px 0; border-radius: 4px;'>
                <p style='color: #856404; font-size: 14px; margin: 0;'>
                    ⚠️ <strong>Lưu ý:</strong> Link xác thực sẽ hết hạn sau <strong>24 giờ</strong>. 
                    Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.
                </p>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8f9fa; padding: 25px; text-align: center; border-top: 1px solid #eeeeee;'>
            <p style='color: #888888; font-size: 13px; margin: 0 0 10px 0;'>
                © 2024 LaptopStore. Mọi quyền được bảo lưu.
            </p>
            <p style='color: #aaaaaa; font-size: 12px; margin: 0;'>
                Đây là email tự động, vui lòng không trả lời email này.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private static string GenerateVerificationSuccessEmailBody(string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 40px 20px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>� LaptopStore</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px;'>Xác thực thành công!</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px 30px; text-align: center;'>
            <div style='font-size: 60px; margin-bottom: 20px;'>🎉</div>
            
            <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 24px;'>Chúc mừng {userName}!</h2>
            
            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                Tài khoản của bạn đã được xác thực thành công. Bây giờ bạn có thể đăng nhập và trải nghiệm 
                đầy đủ các dịch vụ của LaptopStore.
            </p>
            
            <div style='background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%); padding: 25px; border-radius: 12px; margin: 30px 0;'>
                <h3 style='color: #333; margin: 0 0 15px 0; font-size: 18px;'>Bạn có thể:</h3>
                <ul style='color: #666; font-size: 14px; text-align: left; line-height: 2; margin: 0; padding-left: 20px;'>
                    <li>Mua sắm Laptop và phụ kiện chính hãng</li>
                    <li>Theo dõi đơn hàng của bạn</li>
                    <li>Nhận thông báo về khuyến mãi hấp dẫn</li>
                    <li>Đánh giá sản phẩm sau khi mua hàng</li>
                </ul>
            </div>
            
            <a href='/' 
               style='display: inline-block; background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); 
                      color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; 
                      font-size: 16px; font-weight: bold; box-shadow: 0 4px 15px rgba(17, 153, 142, 0.4);'>
                � Bắt đầu mua sắm
            </a>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8f9fa; padding: 25px; text-align: center; border-top: 1px solid #eeeeee;'>
            <p style='color: #888888; font-size: 13px; margin: 0 0 10px 0;'>
                © 2024 LaptopStore. Mọi quyền được bảo lưu.
            </p>
            <p style='color: #aaaaaa; font-size: 12px; margin: 0;'>
                Đây là email tự động, vui lòng không trả lời email này.
            </p>
        </div>
    </div>
</body>
</html>";
        }
        private static string GenerateAccountSetupEmailBody(string userName, string setupLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #FF6B6B 0%, #556270 100%); padding: 40px 20px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>💻 LaptopStore</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px;'>Thiết lập tài khoản của bạn</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 22px;'>Xin chào {userName}! 👋</h2>
            
            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                Quản trị viên đã tạo tài khoản cho bạn tại hệ thống LaptopStore. Để bắt đầu sử dụng, 
                vui lòng thiết lập mật khẩu của bạn bằng cách nhấp vào nút bên dưới.
            </p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <a href='{setupLink}' 
                   style='display: inline-block; background: linear-gradient(135deg, #FF6B6B 0%, #556270 100%); 
                          color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; 
                          font-size: 16px; font-weight: bold; box-shadow: 0 4px 15px rgba(85, 98, 112, 0.4);'>
                    Thiết lập mật khẩu
                </a>
            </div>
            
            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;'>
                Sau khi thiết lập mật khẩu, bạn cũng có thể cập nhật thông tin cá nhân (Số điện thoại, Địa chỉ) 
                trong phần Hồ sơ cá nhân.
            </p>

            <p style='color: #888888; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                Hoặc copy và dán link sau vào trình duyệt:
            </p>
            <p style='color: #556270; font-size: 13px; word-break: break-all; background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 0 0 25px 0;'>
                {setupLink}
            </p>
            
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 25px 0; border-radius: 4px;'>
                <p style='color: #856404; font-size: 14px; margin: 0;'>
                    ⚠️ <strong>Lưu ý:</strong> Link này sẽ hết hạn sau <strong>24 giờ</strong>.
                </p>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8f9fa; padding: 25px; text-align: center; border-top: 1px solid #eeeeee;'>
            <p style='color: #888888; font-size: 13px; margin: 0 0 10px 0;'>
                © 2024 LaptopStore. Mọi quyền được bảo lưu.
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
