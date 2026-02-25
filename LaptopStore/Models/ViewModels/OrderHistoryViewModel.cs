namespace LaptopStore.Models.ViewModels
{
    public class OrderHistoryViewModel
    {
            public int OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal TotalMoney { get; set; }
            public string Status { get; set; }
            public string PaymentMethod { get; set; }
            public string PaymentStatus { get; set; }
        }
    }
