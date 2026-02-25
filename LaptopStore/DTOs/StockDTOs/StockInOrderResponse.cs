namespace LaptopStore.DTOs.StockDTOs;

public class StockInOrderResponse
{
    public int ReceiptId { get; set; }
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
    public int DetailId { get; set; }
    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int RequestedQuantity { get; set; }

    public int ActualQuantity { get; set; }

    public decimal? ImportPrice { get; set; }

}
