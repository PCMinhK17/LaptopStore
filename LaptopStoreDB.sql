CREATE DATABASE LaptopStoreDB;
GO
USE LaptopStoreDB;
GO

-- 1. Bảng Users
CREATE TABLE Users (
    id INT PRIMARY KEY IDENTITY(1,1),
    email VARCHAR(100) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone_number VARCHAR(15) UNIQUE NOT NULL,
    address NVARCHAR(255),
    avatar_url VARCHAR(255),
    role VARCHAR(20) NOT NULL DEFAULT 'customer' CHECK (role IN ('admin', 'staff', 'customer')),
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'banned', 'pending')),
	ban_reason NVARCHAR(255),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- 2. Bảng Categories
CREATE TABLE Categories (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX),
    is_active BIT NOT NULL DEFAULT 1
);

-- 3. Bảng Brands
CREATE TABLE Brands (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    logo_url VARCHAR(255) NOT NULL,
    origin NVARCHAR(100) NOT NULL
);

-- 4. Bảng Products
CREATE TABLE Products (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(255) NOT NULL,
    sku VARCHAR(50) UNIQUE NOT NULL,
    category_id INT FOREIGN KEY REFERENCES Categories(id) ON DELETE SET NULL,
    brand_id INT FOREIGN KEY REFERENCES Brands(id) ON DELETE SET NULL,
    price DECIMAL(15, 2) NOT NULL CHECK (price >= 0),
    old_price DECIMAL(15, 2) CHECK (old_price >= 0),
    stock_quantity INT NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    description NVARCHAR(MAX),
    short_description NVARCHAR(500),
    cpu NVARCHAR(100) NOT NULL,
    ram NVARCHAR(50) NOT NULL,
    hard_drive NVARCHAR(100) NOT NULL,
    gpu NVARCHAR(100),
    screen_size NVARCHAR(50) NOT NULL,
    weight NVARCHAR(50) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- 5. Bảng Product Images
CREATE TABLE Product_Images (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(id) ON DELETE CASCADE,
    image_url VARCHAR(255) NOT NULL,
    is_thumbnail BIT NOT NULL DEFAULT 0
);

-- 6. Bảng Coupons
CREATE TABLE Coupons (
    id INT PRIMARY KEY IDENTITY(1,1),
    code VARCHAR(50) UNIQUE NOT NULL,
    discount_type VARCHAR(20) NOT NULL CHECK (discount_type IN ('fixed_amount', 'percentage')),
    discount_value DECIMAL(15, 2) NOT NULL,
    max_discount_amount DECIMAL(15, 2) NOT NULL,
    min_order_value DECIMAL(15, 2) NOT NULL DEFAULT 0,
    usage_limit INT NOT NULL,
    usage_count INT NOT NULL DEFAULT 0,
    start_date DATETIME,
    end_date DATETIME NOT NULL,
    is_active BIT NOT NULL DEFAULT 1
);

-- 7. Bảng Orders
CREATE TABLE Orders (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NULL FOREIGN KEY REFERENCES Users(id) ON DELETE SET NULL,
    subtotal DECIMAL(15, 2) NOT NULL,
    coupon_code VARCHAR(50),
    discount_amount DECIMAL(15, 2) NOT NULL DEFAULT 0,
    total_money DECIMAL(15, 2) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone_number VARCHAR(15) NOT NULL,
    address NVARCHAR(255) NOT NULL,
    note NVARCHAR(MAX),
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'shipping', 'completed', 'cancelled')),
    payment_method VARCHAR(20) DEFAULT 'cod' CHECK (payment_method IN ('cod', 'qr')),
    payment_status VARCHAR(20) DEFAULT 'unpaid' CHECK (payment_status IN ('unpaid', 'paid', 'refunded')),
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- 8. Bảng Order Details
CREATE TABLE Order_Details (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_id INT NOT NULL FOREIGN KEY REFERENCES Orders(id) ON DELETE CASCADE,
    product_id INT NULL FOREIGN KEY REFERENCES Products(id) ON DELETE SET NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    price DECIMAL(15, 2) NOT NULL,
    total_price AS (quantity * price)
);

-- 9. Bảng Import Receipts
CREATE TABLE Import_Receipts (
    id INT PRIMARY KEY IDENTITY(1,1),
    staff_id INT FOREIGN KEY REFERENCES Users(id) ON DELETE SET NULL,
    supplier_name NVARCHAR(100) NOT NULL,
    total_cost DECIMAL(15, 2) NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'cancel', 'success')),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    delivered_at DATETIME NULL
);

-- 10. Bảng Import Details
CREATE TABLE Import_Details (
    id INT PRIMARY KEY IDENTITY(1,1),
    receipt_id INT NOT NULL FOREIGN KEY REFERENCES Import_Receipts(id) ON DELETE CASCADE,
    product_id INT FOREIGN KEY REFERENCES Products(id) ON DELETE SET NULL,
    requested_quantity INT NOT NULL CHECK (requested_quantity >= 0),
    actual_quantity INT NOT NULL CHECK (actual_quantity >= 0),
    import_price DECIMAL(15, 2) NOT NULL CHECK (import_price >= 0)
);

-- 11. Bảng Reviews (Đã xóa cột is_approved)
CREATE TABLE Reviews (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(id) ON DELETE CASCADE,
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(id) ON DELETE CASCADE,
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    comment NVARCHAR(MAX) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);

-- 12. Bảng Carts & Cart Items
CREATE TABLE Carts (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT UNIQUE NOT NULL FOREIGN KEY REFERENCES Users(id) ON DELETE CASCADE,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);

CREATE TABLE Cart_Items (
    id INT PRIMARY KEY IDENTITY(1,1),
    cart_id INT NOT NULL FOREIGN KEY REFERENCES Carts(id) ON DELETE CASCADE,
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 1 CHECK (quantity > 0),
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UK_Cart_Product UNIQUE(cart_id, product_id)
);

-- 13. Bảng Notifications & Verification
CREATE TABLE Notifications (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(id) ON DELETE CASCADE,
    title NVARCHAR(255) NOT NULL,
    message NVARCHAR(MAX),
    is_read BIT NOT NULL DEFAULT 0,
    type VARCHAR(50) DEFAULT 'order' CHECK (type IN('order', 'receipt')),
    created_at DATETIME DEFAULT GETDATE()
);

CREATE TABLE Email_Verification_Tokens (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(id) ON DELETE CASCADE,
    token VARCHAR(100) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    expires_at DATETIME NOT NULL,
    is_used BIT NOT NULL DEFAULT 0
);

-- 14. Bảng Wishlists
CREATE TABLE [Wishlists] (
    [id] int NOT NULL IDENTITY(1,1),
    [user_id] int NULL UNIQUE,
    [created_at] datetime NULL DEFAULT (getdate()),
    CONSTRAINT [PK_Wishlists] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Wishlists_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [Wishlist_Items] (
    [id] int NOT NULL IDENTITY(1,1),
    [wishlist_id] int NULL,
    [product_id] int NULL,
    [created_at] datetime NULL DEFAULT (getdate()),
    CONSTRAINT [PK_Wishlist_Items] PRIMARY KEY ([id]),
    CONSTRAINT [FK_Wishlist_Items_Products_product_id] FOREIGN KEY ([product_id]) REFERENCES [Products] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Wishlist_Items_Wishlists_wishlist_id] FOREIGN KEY ([wishlist_id]) REFERENCES [Wishlists] ([id]) ON DELETE CASCADE
);
GO


USE LaptopStoreDB;
GO

-- 1. Users
INSERT INTO Users (email, password, full_name, phone_number, address, role) VALUES 
('admin@store.com', 'hash_admin_123', N'Nguyễn Quản Lý', '0909000001', N'Hà Nội', 'admin'),
('staff1@gmail.com', 'hash_staff_1', N'Trần Nhân Viên 1', '0910000001', N'Sao Hỏa', 'staff'),
('staff2@gmail.com', 'hash_staff_12', N'Phạm Nhân Viên 2', '0910000002', N'Sao Hỏa', 'staff'),
('user1@gmail.com', 'hash_pass_1', N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'customer'),
('user2@gmail.com', 'hash_pass_2', N'Lê Văn Khách 2', '0912000002', N'Đà Nẵng', 'customer'),
('user3@gmail.com', 'hash_pass_3', N'Phạm Thị C', '0912000003', N'Cần Thơ', 'customer'),
('user4@gmail.com', 'hash_pass_4', N'Hoàng Văn D', '0912000004', N'Hải Phòng', 'customer'),
('user5@gmail.com', 'hash_pass_5', N'Vũ Thị E', '0912000005', N'Hà Nội', 'customer'),
('user6@gmail.com', 'hash_pass_6', N'Đặng Văn F', '0912000006', N'Nghệ An', 'customer'),
('user7@gmail.com', 'hash_pass_7', N'Bùi Thị G', '0912000007', N'Thanh Hóa', 'customer'),
('user8@gmail.com', 'hash_pass_8', N'Đỗ Văn H', '0912000008', N'Quảng Ninh', 'customer'),
('user9@gmail.com', 'hash_pass_9', N'Ngô Thị I', '0912000009', N'Huế', 'customer');

-- 2. Categories
INSERT INTO Categories (name, description) VALUES 
(N'Laptop Gaming', N'Cấu hình mạnh mẽ chiến game'),
(N'Laptop Văn Phòng', N'Mỏng nhẹ, pin trâu'),
(N'Laptop Đồ Họa', N'Màn hình chuẩn màu, cấu hình cao'),
(N'MacBook', N'Sản phẩm Apple'),
(N'Ultrabook', N'Siêu mỏng nhẹ cao cấp'),
(N'Laptop Sinh Viên', N'Giá rẻ, bền bỉ'),
(N'Workstation', N'Trạm làm việc chuyên nghiệp'),
(N'Laptop 2-in-1', N'Xoay gập cảm ứng'),
(N'Phụ kiện Laptop', N'Chuột, phím, tai nghe'),
(N'Linh kiện Laptop', N'Ram, SSD, Màn hình');

-- 3. Brands (Đã bổ sung logo_url NOT NULL)
INSERT INTO Brands (name, logo_url, origin) VALUES 
('Dell', '/images/logos/dell.png', 'USA'), ('Asus', '/images/logos/asus.jpg', 'Taiwan'), 
('HP', '/images/logos/hp.png', 'USA'), ('Apple', '/images/logos/apple.png', 'USA'), 
('Lenovo', '/images/logos/lenovo.png', 'China'), ('MSI', '/images/logos/msi.jpg', 'Taiwan'), 
('Acer', '/images/logos/acer.png', 'Taiwan'), ('LG', '/images/logos/lg.png', 'Korea'), 
('Gigabyte', '/images/logos/gigabyte.png', 'Taiwan'), ('Razer', '/images/logos/razer.png', 'USA');

-- 4. Products (Đã bổ sung weight NOT NULL)
INSERT INTO Products (name, sku, category_id, brand_id, price, old_price, stock_quantity, cpu, ram, hard_drive, gpu, screen_size, weight) VALUES 
(N'Dell XPS 13 9310', 'DEL-XPS-01', 2, 1, 25000000, 27000000, 10, 'i7 1165G7', '16GB', '512GB SSD', 'Iris Xe', '13.4 inch', '1.2 kg'),
(N'Asus ROG Strix G15', 'ASU-ROG-01', 1, 2, 32000000, 35000000, 5, 'Ryzen 7 6800H', '16GB', '1TB SSD', 'RTX 3060', '15.6 inch', '2.3 kg'),
(N'MacBook Air M1', 'APP-AIR-M1', 4, 4, 18000000, 20000000, 20, 'Apple M1', '8GB', '256GB SSD', '7-core GPU', '13.3 inch', '1.29 kg'),
(N'HP Spectre x360', 'HP-SPEC-01', 5, 3, 29000000, 31000000, 3, 'i7 1255U', '16GB', '1TB SSD', 'Iris Xe', '14 inch', '1.36 kg'),
(N'Lenovo Legion 5', 'LEN-LEG-05', 1, 5, 27500000, 30000000, 8, 'Ryzen 5 5600H', '16GB', '512GB SSD', 'RTX 3050Ti', '15.6 inch', '2.4 kg'),
(N'Acer Nitro 5', 'ACE-NIT-05', 1, 7, 19000000, 22000000, 15, 'i5 11400H', '8GB', '512GB SSD', 'GTX 1650', '15.6 inch', '2.2 kg'),
(N'MSI Modern 14', 'MSI-MOD-14', 6, 6, 14000000, 16000000, 12, 'i3 1115G4', '8GB', '256GB SSD', 'UHD Graphics', '14 inch', '1.3 kg'),
(N'LG Gram 17', 'LG-GRA-17', 2, 8, 35000000, 38000000, 4, 'i7 1260P', '16GB', '1TB SSD', 'Iris Xe', '17 inch', '1.35 kg'),
(N'Gigabyte G5', 'GIG-G5-01', 1, 9, 21000000, 24000000, 7, 'i5 11400H', '16GB', '512GB SSD', 'RTX 3050', '15.6 inch', '2.2 kg'),
(N'MacBook Pro 14 M1 Pro', 'APP-PRO-14', 4, 4, 45000000, 50000000, 6, 'M1 Pro', '16GB', '512GB SSD', '14-core GPU', '14.2 inch', '1.6 kg');

-- 5. Product Images
INSERT INTO Product_Images (product_id, image_url, is_thumbnail) VALUES 
(1, '/images/dell-xps-1.jpg', 1), (1, '/images/dell-xps-2.jpg', 0),
(2, '/images/asus-rog-1.jpg', 1), (2, '/images/asus-rog-2.jpg', 0),
(3, '/images/mac-m1-1.jpg', 1), (4, '/images/hp-spectre-1.jpg', 1),
(5, '/images/lenovo-legion-1.jpg', 1), (6, '/images/acer-nitro-1.jpg', 1),
(7, '/images/msi-modern-1.jpg', 1), (8, '/images/lg-gram-1.jpg', 1),
(9, '/images/gigabyte-g5-1.jpg', 1), (10, '/images/macbook-m1-1.jpg', 1);

-- 6. Coupons (Đã bổ sung max_discount_amount NOT NULL)
INSERT INTO Coupons (code, discount_type, discount_value, max_discount_amount, min_order_value, usage_limit, end_date) VALUES 
('SALE10', 'percentage', 10, 500000, 5000000, 100, '2025-12-31'),
('GIAM500K', 'fixed_amount', 500000, 500000, 10000000, 50, '2025-12-31'),
('TET2024', 'percentage', 15, 1000000, 0, 200, '2025-02-15'),
('FREESHIP', 'fixed_amount', 30000, 30000, 2000000, 1000, '2025-12-31'),
('BLACKFRIDAY', 'percentage', 20, 2000000, 0, 10, '2024-11-29');

-- 7. Orders (Đơn hàng) 
-- Sửa user_id đơn hàng vãng lai từ NULL -> 10 để tránh lỗi NOT NULL Constraint
INSERT INTO Orders (user_id, subtotal, total_money, full_name, phone_number, address, status, payment_method, payment_status) VALUES 
(4, 25000000, 25000000, N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'completed', 'qr', 'paid'),
(3, 32000000, 32050000, N'Lê Văn Khách 2', '0912000002', N'Đà Nẵng', 'pending', 'cod', 'unpaid'),
(10, 18000000, 18100000, N'Nguyễn Văn Vãng Lai', '0988888888', N'Hà Giang', 'shipping', 'cod', 'unpaid'), 
(4, 29000000, 28000000, N'Phạm Thị C', '0912000003', N'Cần Thơ', 'completed', 'qr', 'paid'),
(5, 27500000, 27500000, N'Hoàng Văn D', '0912000004', N'Hải Phòng', 'cancelled', 'cod', 'unpaid'),
(6, 19000000, 18530000, N'Đặng Văn F', '0912000006', N'Nghệ An', 'confirmed', 'qr', 'paid'),
(7, 14000000, 14000000, N'Bùi Thị G', '0912000007', N'Thanh Hóa', 'pending', 'cod', 'unpaid'),
(5, 35000000, 35000000, N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'pending', 'qr', 'unpaid'), 
(8, 21000000, 21050000, N'Đỗ Văn H', '0912000008', N'Quảng Ninh', 'shipping', 'cod', 'unpaid'),
(9, 45000000, 45000000, N'Ngô Thị I', '0912000009', N'Huế', 'completed', 'qr', 'paid');

-- 8. Order Details
INSERT INTO Order_Details (order_id, product_id, quantity, price) VALUES 
(1, 1, 1, 25000000), (2, 2, 1, 32000000), (3, 3, 1, 18000000), 
(4, 4, 1, 29000000), (5, 5, 1, 27500000), (6, 6, 1, 19000000), 
(7, 7, 1, 14000000), (8, 8, 1, 35000000), (9, 9, 1, 21000000), (10, 10, 1, 45000000);

-- 9. Import Receipts
INSERT INTO Import_Receipts (staff_id, supplier_name, total_cost, status) VALUES 
(2, N'FPT Trading', 212000000, 'success'),
(3, N'Digiworld', 300000000, 'success'),
(2, N'Viễn Sơn', 150000000, 'success');

-- 10. Import Details
INSERT INTO Import_Details (receipt_id, product_id, requested_quantity, actual_quantity, import_price) VALUES 
(1, 1, 10, 10, 20000000), 
(1, 2, 5, 4, 28000000),
(2, 3, 20, 20, 15000000),
(3, 4, 3, 2, 25000000);

-- 11. Reviews (Đã xóa cột is_approved ở cuối)
INSERT INTO Reviews (user_id, product_id, rating, comment) VALUES 
(4, 1, 5, N'Máy rất đẹp, mỏng nhẹ, đáng tiền!'),
(3, 2, 4, N'Máy mạnh nhưng quạt hơi ồn khi chơi game nặng.'),
(4, 3, 5, N'Pin trâu dã man, dùng cả ngày không hết.'),
(5, 4, 5, N'Màn hình cảm ứng mượt, xoay gập tiện lợi.');

-- 12. Carts & Cart Items
INSERT INTO Carts (user_id) VALUES (5), (3), (4);

INSERT INTO Cart_Items (cart_id, product_id, quantity) VALUES 
(1, 1, 1), -- Giỏ của User 5 có Dell XPS
(1, 9, 2), -- Giỏ của User 5 có Gigabyte G5
(2, 3, 1); -- Giỏ của User 3 có Mac M1

-- 13. Notifications (Sửa type thành 'order' và 'receipt' cho đúng Constraint)
INSERT INTO Notifications (user_id, title, message, type, is_read, created_at) VALUES 
(3, N'Đặt hàng thành công', N'Đơn hàng #1 của bạn đã được ghi nhận.', 'order', 1, GETDATE()),
(3, N'Đang giao hàng', N'Đơn hàng #1 đang được giao bởi Shipper.', 'order', 0, GETDATE()),
(4, N'Cập nhật hóa đơn', N'Hóa đơn mua hàng đợt Tết.', 'order', 0, GETDATE()), 
(5, N'Nhập kho thành công', N'Phiếu nhập #1 đã hoàn tất.', 'receipt', 0, GETDATE());

-- 14. BỔ SUNG: Dữ liệu mẫu cho bảng Email_Verification_Tokens
INSERT INTO Email_Verification_Tokens (user_id, token, expires_at, is_used) VALUES 
(3, 'token_xac_nhan_123', DATEADD(DAY, 1, GETDATE()), 1),
(6, 'token_xac_nhan_456', DATEADD(DAY, 1, GETDATE()), 0);

-- 15. BỔ SUNG: Dữ liệu mẫu cho Wishlists & Wishlist_Items
INSERT INTO Wishlists (user_id) VALUES (4), (5), (6);

INSERT INTO Wishlist_Items (wishlist_id, product_id) VALUES 
(1, 1),  -- User 4 thích Dell XPS
(1, 10), -- User 4 thích Mac Pro
(2, 2);  -- User 5 thích Asus ROG
GO