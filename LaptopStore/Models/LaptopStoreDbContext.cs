using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Models;

public partial class LaptopStoreDbContext : DbContext
{
    public LaptopStoreDbContext()
    {
    }

    public LaptopStoreDbContext(DbContextOptions<LaptopStoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }
    public virtual DbSet<ImportDetail> ImportDetails { get; set; }

    public virtual DbSet<ImportReceipt> ImportReceipts { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<WishlistItem> WishlistItems { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = new ConfigurationBuilder()
                       .SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfigurationRoot configuration = builder.Build();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("MyCnn"));

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Brands__3213E83F1656AC90");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Origin)
                .HasMaxLength(100)
                .HasColumnName("origin");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Carts__3213E83F221F461A");

            entity.HasIndex(e => e.UserId, "UQ__Carts__B9BE370EA3737CB7").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .HasConstraintName("FK__Carts__user_id__6477ECF3");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart_Ite__3213E83F313BF6C8");

            entity.ToTable("Cart_Items");

            entity.HasIndex(e => new { e.CartId, e.ProductId }, "UK_Cart_Product").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__Cart_Item__cart___6A30C649");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Cart_Item__produ__6B24EA82");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3213E83F8CA0BC9F");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Coupons__3213E83FEC4909AB");

            entity.HasIndex(e => e.Code, "UQ__Coupons__357D4CF9C4B45CC5").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinOrderValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("min_order_value");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.UsageCount)
                .HasDefaultValue(0)
                .HasColumnName("usage_count");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
        });

        modelBuilder.Entity<ImportDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Import_D__3213E83F4FFE579F");

            entity.ToTable("Import_Details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualQuantity).HasColumnName("actual_quantity");
            entity.Property(e => e.ImportPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("import_price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.RequestedQuantity).HasColumnName("requested_quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.ImportDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Import_De__produ__59FA5E80");

            entity.HasOne(d => d.Receipt).WithMany(p => p.ImportDetails)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("FK__Import_De__recei__59063A47");
        });

        modelBuilder.Entity<ImportReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Import_R__3213E83F2E754923");

            entity.ToTable("Import_Receipts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveredAt)
                .HasColumnType("datetime")
                .HasColumnName("delivered_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(100)
                .HasColumnName("supplier_name");
            entity.Property(e => e.TotalCost)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("total_cost");

            entity.HasOne(d => d.Staff).WithMany(p => p.ImportReceipts)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__Import_Re__staff__534D60F1");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3213E83F4E9F794F");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("system")
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__user___70DDC3D8");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3213E83F4B96D80B");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.CouponCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("coupon_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("cod")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("unpaid")
                .HasColumnName("payment_status");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.TotalMoney)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("total_money");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Orders__user_id__44FF419A");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order_De__3213E83F06AE2E46");

            entity.ToTable("Order_Details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([quantity]*[price])", false)
                .HasColumnType("decimal(26, 2)")
                .HasColumnName("total_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Order_Det__order__4F7CD00D");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Order_Det__produ__5070F446");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3213E83F6B93100F");

            entity.HasIndex(e => e.Sku, "UQ__Products__DDDF4BE7DDAEF14A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Cpu)
                .HasMaxLength(100)
                .HasColumnName("cpu");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Gpu)
                .HasMaxLength(100)
                .HasColumnName("gpu");
            entity.Property(e => e.HardDrive)
                .HasMaxLength(100)
                .HasColumnName("hard_drive");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.OldPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("old_price");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Ram)
                .HasMaxLength(50)
                .HasColumnName("ram");
            entity.Property(e => e.ScreenSize)
                .HasMaxLength(50)
                .HasColumnName("screen_size");
            entity.Property(e => e.ShortDescription)
                .HasMaxLength(500)
                .HasColumnName("short_description");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("stock_quantity");
            entity.Property(e => e.Weight)
                .HasMaxLength(50)
                .HasColumnName("weight");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK__Products__brand___34C8D9D1");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__catego__33D4B598");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product___3213E83FA8081A6B");

            entity.ToTable("Product_Images");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.IsThumbnail)
                .HasDefaultValue(false)
                .HasColumnName("is_thumbnail");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Product_I__produ__3A81B327");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3213E83F43A506BB");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(true)
                .HasColumnName("is_approved");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Reviews__product__5DCAEF64");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__user_id__5CD6CB2B");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83FA19A77CF");

            entity.HasIndex(e => e.PhoneNumber, "UQ__Users__A1936A6BB8BC7BA3").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164BDF3686F").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("customer")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Email_Verification_Tokens");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Token)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("token");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmailVerificationTokens_Users");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Wishlist)
                .HasForeignKey<Wishlist>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("Wishlist_Items");

            entity.HasIndex(e => new { e.WishlistId, e.ProductId }).IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Wishlist).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.WishlistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product).WithMany(p => p.WishlistItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

