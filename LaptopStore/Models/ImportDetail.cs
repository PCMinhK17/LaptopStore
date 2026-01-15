using System;
using System.Collections.Generic;

namespace LaptopStore.Models;

public partial class ImportDetail
{
    public int Id { get; set; }

    public int? ReceiptId { get; set; }

    public int? ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal ImportPrice { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ImportReceipt? Receipt { get; set; }
}
