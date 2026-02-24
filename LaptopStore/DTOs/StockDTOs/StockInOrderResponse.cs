namespace LaptopStore.DTOs.StockDTOs;

public class StockInOrderResponse
{
    public string SupplierName { get; set; } = "";

    public int StaffName { get; set; }

    public string Status { get; set; } = "";

    public decimal? TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public List<StockInItemResponse> Items { get; set; } = [];
}

public class  StockInItemResponse
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int RequestedQuantity { get; set; }

    public int ActualQuantity { get; set; }

    public decimal? ImportPrice { get; set; }

}
