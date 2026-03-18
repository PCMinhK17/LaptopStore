namespace LaptopStore.DTOs.StockDTOs;

public class StockInOrderResponse
{
    public int Id { get; set; }

    public string SupplierName { get; set; } = "";

    public string StaffName { get; set; } = "Không thấy";

    public string StaffAvatarUrl { get; set; } = "/images/image-not-found";

    public string StaffEmail { get; set; } = "Không thấy";

    public string Status { get; set; } = "";

    public decimal? TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public List<StockInItemResponse> Items { get; set; } = [];
}

public class  StockInItemResponse
{
    public int DetailId { get; set; }
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public int RequestedQuantity { get; set; }

    public int ActualQuantity { get; set; }

    public decimal? ImportPrice { get; set; }

}
