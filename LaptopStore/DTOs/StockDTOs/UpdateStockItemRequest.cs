using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.StockDTOs
{
    public class UpdateStockItemRequest
    {
        public int DetailId { get; set; }

        [Range(0, 999, ErrorMessage = "Thực nhập phải nằm trong khoảng từ 0 đến 999.")]

        public int ActualQuantity { get; set; }
        public decimal ImportPrice { get; set; }
    }
}
