using LaptopStore.Models;

namespace LaptopStore.DTOs.ProductDTOs;

public class ProductResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int? StockQuantity { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? Cpu { get; set; }

    public string? Ram { get; set; }

    public string? HardDrive { get; set; }

    public string? Gpu { get; set; }

    public string? ScreenSize { get; set; }

    public string? Weight { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual string? BrandName { get; set; }

    public virtual string? CategoryName { get; set; }

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

}
