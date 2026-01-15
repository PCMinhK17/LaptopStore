using System;
using System.Collections.Generic;

namespace LaptopStore.Models;

public partial class ImportReceipt
{
    public int Id { get; set; }

    public int? AdminId { get; set; }

    public string? SupplierName { get; set; }

    public decimal? TotalCost { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Admin { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
}
