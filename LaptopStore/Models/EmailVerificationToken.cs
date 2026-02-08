using System;

namespace LaptopStore.Models;

/// <summary>
/// Lưu trữ token xác thực email cho user
/// </summary>
public class EmailVerificationToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Token ngẫu nhiên (GUID) được gửi qua email
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Thời điểm tạo token
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời điểm hết hạn (mặc định 24 giờ)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Đã sử dụng hay chưa
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual User User { get; set; } = null!;
}
