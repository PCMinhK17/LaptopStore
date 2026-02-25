namespace LaptopStore.DTOs.StockDTOs;

public class StockInOrderRequest
{
    public string SupplierName { get; set; } = "";

    public int StaffId { get; set; }

    public List<StockInItemRequest> Items { get; set; } = new List<StockInItemRequest>();
}

public class StockInItemRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}