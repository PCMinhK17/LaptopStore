namespace LaptopStore.DTOs.StockDTOs
{
    public class UpdateStockRequest
    {
        public int StockId { get; set; }

        public List<UpdateStockItemRequest> Items { get; set; } = new List<UpdateStockItemRequest>();
    }
}
