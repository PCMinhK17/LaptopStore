using System;
using System.Collections.Generic;

namespace LaptopStore.Models;

public partial class ImportReceipt
{
    public int Id { get; set; }

    public int? StaffId { get; set; }

    public string SupplierName { get; set; } = null!;

    public decimal TotalCost { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual User? Staff { get; set; }
}
