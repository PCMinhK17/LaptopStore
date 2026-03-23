using LaptopStore.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

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

        public async Task<bool> SendOrderInformationAsync(string toEmail, string userName, Order order)
        {
            try
            {
                var subject = "Chào mừng đến với LaptopStore - Bạn đã mua đơn hàng mới";
                var body = GenerateOrderInformationBody(userName, order);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email thiết lập tài khoản đến {Email}", toEmail);
                return false;
            }
        }


        private static string GenerateOrderInformationBody(string userName, Order order)
        {
            var sb = new StringBuilder();

            // Bắt đầu thẻ HTML và cấu hình CSS nội bộ
            sb.Append($@"
    <!DOCTYPE html>
    <html lang='vi'>
    <head>
        <meta charset='UTF-8'>
        <style>
            body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f5f7; margin: 0; padding: 20px; }}
            .container {{ max-width: 650px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.05); }}
            .header {{ background: linear-gradient(135deg, #0d6efd 0%, #0a58ca 100%); color: #ffffff; padding: 25px 20px; text-align: center; }}
            .header h2 {{ margin: 0; font-size: 24px; }}
            .content {{ padding: 30px 20px; }}
            .info-box {{ background: #f8f9fa; border: 1px solid #e9ecef; border-radius: 6px; padding: 15px; margin-bottom: 20px; }}
            .info-box p {{ margin: 5px 0; font-size: 14px; }}
            table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
            th, td {{ padding: 12px; border-bottom: 1px solid #dee2e6; text-align: left; font-size: 14px; }}
            th {{ background-color: #f8f9fa; font-weight: 600; color: #495057; }}
            .total-row td {{ font-size: 16px; font-weight: bold; border-bottom: none; }}
            .total-amount {{ color: #0d6efd; font-size: 20px !important; }}
            .footer {{ background: #f8f9fa; text-align: center; padding: 20px; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h2>Xác Nhận Đơn Hàng</h2>
                <p style='margin-top: 5px; opacity: 0.9;'>Cảm ơn bạn đã tin tưởng và mua sắm tại LaptopStore!</p>
            </div>

            <div class='content'>
                <p>Xin chào <strong>{userName}</strong>,</p>
                <p>Đơn hàng của bạn đã được hệ thống ghi nhận thành công. Dưới đây là thông tin chi tiết về hóa đơn của bạn:</p>

                <div class='info-box'>
                    <p><strong>Mã đơn hàng:</strong> <span style='color: #0d6efd;'>#{order.Id}</span></p>
                    <p><strong>Ngày đặt:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod ?? "Chưa xác định"}</p>
                    <p><strong>Trạng thái thanh toán:</strong> {order.PaymentStatus ?? "Chờ xử lý"}</p>
                </div>

                <h4 style='margin-bottom: 10px; color: #212529;'>Thông tin giao hàng</h4>
                <div class='info-box'>
                    <p><strong>Người nhận:</strong> {order.FullName}</p>
                    <p><strong>Số điện thoại:</strong> {order.PhoneNumber}</p>
                    <p><strong>Địa chỉ:</strong> {order.Address}</p>");

            if (!string.IsNullOrEmpty(order.Note))
            {
                sb.Append($"<p><strong>Ghi chú:</strong> {order.Note}</p>");
            }

            sb.Append($@"
                </div>

                <h4 style='margin-bottom: 10px; color: #212529;'>Chi tiết sản phẩm</h4>
                <table>
                    <thead>
                        <tr>
                            <th>Sản phẩm</th>
                            <th style='text-align: center;'>SL</th>
                            <th style='text-align: right;'>Đơn giá</th>
                            <th style='text-align: right;'>Thành tiền</th>
                        </tr>
                    </thead>
                    <tbody>");

            // Duyệt qua danh sách sản phẩm trong OrderDetails
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var item in order.OrderDetails)
                {
                    // Kiểm tra an toàn nếu Product bị null, tránh lỗi NullReferenceException
                    string productName = item.Product != null && !string.IsNullOrEmpty(item.Product.Name)
                                         ? item.Product.Name
                                         : $"Sản phẩm ID: {item.ProductId}";

                    decimal lineTotal = item.TotalPrice ?? (item.Price * item.Quantity);

                    sb.Append($@"
                        <tr>
                            <td>{productName}</td>
                            <td style='text-align: center;'>{item.Quantity}</td>
                            <td style='text-align: right;'>{item.Price:N0}đ</td>
                            <td style='text-align: right; font-weight: 500;'>{lineTotal:N0}đ</td>
                        </tr>");
                }
            }

            // Phần tổng tiền (Subtotal, Giảm giá, Total)
            sb.Append($@"
                    </tbody>
                </table>

                <table style='width: 100%; border: none; margin-top: -10px;'>
                    <tr>
                        <td style='border: none; text-align: right; padding: 5px 12px;'>Tạm tính:</td>
                        <td style='border: none; text-align: right; width: 150px; padding: 5px 12px;'>{order.Subtotal:N0}đ</td>
                    </tr>");

            // Nếu có mã giảm giá thì mới in ra dòng này
            if (order.DiscountAmount > 0)
            {
                string couponText = !string.IsNullOrEmpty(order.CouponCode) ? $" ({order.CouponCode})" : "";
                sb.Append($@"
                    <tr>
                        <td style='border: none; text-align: right; padding: 5px 12px;'>Giảm giá{couponText}:</td>
                        <td style='border: none; text-align: right; color: #dc3545; padding: 5px 12px;'>-{order.DiscountAmount:N0}đ</td>
                    </tr>");
            }

            sb.Append($@"
                    <tr class='total-row'>
                        <td style='border: none; text-align: right; padding-top: 15px;'>Tổng thanh toán:</td>
                        <td class='total-amount' style='border: none; text-align: right; padding-top: 15px;'>{order.TotalMoney:N0}đ</td>
                    </tr>
                </table>

            </div>

            <div class='footer'>
                <p style='margin: 0;'>Đây là email thông báo tự động, vui lòng không trả lời email này.</p>
                <p style='margin: 5px 0 0 0;'>&copy; {DateTime.Now.Year} LaptopStore. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>");

            return sb.ToString();
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
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
        {
            try
            {
                var subject = "Đặt lại mật khẩu LaptopStore";
                var body = GeneratePasswordResetEmailBody(userName, resetLink);

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email reset mật khẩu đến {Email}", toEmail);
                return false;
            }
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
        private static string GeneratePasswordResetEmailBody(string userName, string resetLink)
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
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px;'>Khôi phục mật khẩu</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px 30px;'>
            <h2 style='color: #333333; margin: 0 0 20px 0; font-size: 22px;'>Xin chào {userName},</h2>
            
            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>
                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. 
                Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.
            </p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <a href='{resetLink}' 
                   style='display: inline-block; background: linear-gradient(135deg, #FF6B6B 0%, #556270 100%); 
                          color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; 
                          font-size: 16px; font-weight: bold; box-shadow: 0 4px 15px rgba(85, 98, 112, 0.4);'>
                    Đặt lại mật khẩu
                </a>
            </div>
            
            <p style='color: #888888; font-size: 14px; line-height: 1.6; margin: 0 0 10px 0;'>
                Hoặc copy và dán link sau vào trình duyệt:
            </p>
            <p style='color: #556270; font-size: 13px; word-break: break-all; background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 0 0 25px 0;'>
                {resetLink}
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
