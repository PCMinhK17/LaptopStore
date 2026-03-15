using System;
using System.Collections.Generic;

namespace LaptopStore.Models;

public partial class WishlistItem
{
    public int Id { get; set; }

    public int? WishlistId { get; set; }

    public int? ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Wishlist? Wishlist { get; set; }

    public virtual Product? Product { get; set; }
}
