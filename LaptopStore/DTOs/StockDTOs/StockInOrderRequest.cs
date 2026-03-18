using LaptopStore.DTOs.ProductDTOs;

namespace LaptopStore.DTOs.StockDTOs;

public class StockInOrderRequest
{
    public string SupplierName { get; set; } = "";

    public int StaffId { get; set; }

    public string StaffName { get; set; }

    public List<StockInItemRequest> Items { get; set; } = new List<StockInItemRequest>();
}

public class StockInItemRequest
{
    public ProductResponse Product { get; set; }

    public int Quantity { get; set; }
}

public class StockInItemDto
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

    public string? BrandName { get; set; }

    public string? CategoryName { get; set; }

    public ICollection<ProductImageResponse> ProductImages { get; set; } = [];

    public int Quantity { get; set; }
}