using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.ProductDTOs;

public class UpdateProductRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng điền tên cho laptop")]
    [StringLength(100, ErrorMessage = "Tên không được dài quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Mã vạch cho laptop")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Mã vạch phải từ 3 đến 50 ký tự")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phân loại cho laptop")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu cho laptop")]
    public int? BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá cho laptop")]
    public string? Price { get; set; }
    [StringLength(1000, ErrorMessage = "Mô tả chi tiết không được quá 1000 ký tự")]
    public string? Description { get; set; }
    [StringLength(500, ErrorMessage = "Mô tả ngắn không được quá 500 ký tự")]
    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CPU cho laptop")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "CPU phải từ 2 đến 100 ký tự")]
    public string? Cpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập RAM cho laptop")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "RAM phải từ 2 đến 50 ký tự")]
    public string? Ram { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Ổ cứng cho laptop")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ổ cứng phải từ 2 đến 100 ký tự")]
    public string? HardDrive { get; set; }
    [StringLength(100, ErrorMessage = "GPU không được quá 100 ký tự")]
    public string? Gpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Kích cỡ màn hình cho laptop")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Kích cỡ màn hình phải từ 2 đến 50 ký tự")]
    public string? ScreenSize { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Cân nặng cho laptop")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Cân nặng phải từ 2 đến 50 ký tự")]
    public string? Weight { get; set; }

    public int? StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public IFormFile? ImageFile { get; set; }
}

