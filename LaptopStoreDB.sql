-- 1. TẠO DATABASE
CREATE DATABASE LaptopStoreDB;
GO
USE LaptopStoreDB;
GO

-- 2. TẠO BẢNG (DDL)

-- Bảng Users
CREATE TABLE Users (
    id INT PRIMARY KEY IDENTITY(1,1),
    email VARCHAR(100) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL, -- Lưu hash password
    full_name NVARCHAR(100),
    phone_number VARCHAR(15) UNIQUE,
    address NVARCHAR(255),
    role VARCHAR(20) DEFAULT 'customer' CHECK (role IN ('admin', 'staff', 'customer')),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'banned')),
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);

-- Bảng Categories (Danh mục)
CREATE TABLE Categories (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX),
    is_active BIT DEFAULT 1 -- 1: True, 0: False
);

-- Bảng Brands (Thương hiệu)
CREATE TABLE Brands (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(100) NOT NULL,
    logo_url VARCHAR(255),
    origin NVARCHAR(100)
);

-- Bảng Products (Sản phẩm)
CREATE TABLE Products (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVARCHAR(255) NOT NULL,
    sku VARCHAR(50) UNIQUE, -- Mã kho
    category_id INT FOREIGN KEY REFERENCES Categories(id),
    brand_id INT FOREIGN KEY REFERENCES Brands(id),
    price DECIMAL(15, 2) NOT NULL,
    old_price DECIMAL(15, 2),
    stock_quantity INT DEFAULT 0,
    description NVARCHAR(MAX),
    short_description NVARCHAR(500),
    
    -- Thông số kỹ thuật (Filter)
    cpu NVARCHAR(100),
    ram NVARCHAR(50),
    hard_drive NVARCHAR(100),
    gpu NVARCHAR(100),
    screen_size NVARCHAR(50),
    weight NVARCHAR(50),
    
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);

-- Bảng Product Images
CREATE TABLE Product_Images (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT FOREIGN KEY REFERENCES Products(id),
    image_url VARCHAR(255) NOT NULL,
    is_thumbnail BIT DEFAULT 0
);

-- Bảng Coupons (Mã giảm giá)
CREATE TABLE Coupons (
    id INT PRIMARY KEY IDENTITY(1,1),
    code VARCHAR(50) UNIQUE NOT NULL,
    discount_type VARCHAR(20) CHECK (discount_type IN ('fixed_amount', 'percentage')),
    discount_value DECIMAL(15, 2) NOT NULL,
    max_discount_amount DECIMAL(15, 2),
    min_order_value DECIMAL(15, 2) DEFAULT 0,
    usage_limit INT,
    usage_count INT DEFAULT 0,
    start_date DATETIME,
    end_date DATETIME,
    is_active BIT DEFAULT 1
);

-- Bảng Orders (Đơn hàng)
CREATE TABLE Orders (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NULL FOREIGN KEY REFERENCES Users(id), -- Null nếu khách vãng lai
    
    subtotal DECIMAL(15, 2),
    shipping_fee DECIMAL(15, 2) DEFAULT 0,
    coupon_code VARCHAR(50),
    discount_amount DECIMAL(15, 2) DEFAULT 0,
    total_money DECIMAL(15, 2) NOT NULL,
    
    full_name NVARCHAR(100) NOT NULL,
    phone_number VARCHAR(15) NOT NULL,
    address NVARCHAR(255) NOT NULL,
    note NVARCHAR(MAX),
    
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'shipping', 'completed', 'cancelled')),
    payment_method VARCHAR(20) DEFAULT 'cod' CHECK (payment_method IN ('cod', 'vietqr', 'vnpay')),
    payment_status VARCHAR(20) DEFAULT 'unpaid' CHECK (payment_status IN ('unpaid', 'paid', 'refunded')),
    
    created_at DATETIME DEFAULT GETDATE()
);

-- Bảng Order Details
CREATE TABLE Order_Details (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_id INT FOREIGN KEY REFERENCES Orders(id),
    product_id INT FOREIGN KEY REFERENCES Products(id),
    quantity INT NOT NULL,
    price DECIMAL(15, 2) NOT NULL, -- Giá tại thời điểm mua
    total_price AS (quantity * price) -- Computed column (Cột tự tính toán)
);

-- Bảng Import Receipts (Phiếu nhập kho)
CREATE TABLE Import_Receipts (
    id INT PRIMARY KEY IDENTITY(1,1),
    admin_id INT FOREIGN KEY REFERENCES Users(id),
    supplier_name NVARCHAR(100),
    total_cost DECIMAL(15, 2),
    created_at DATETIME DEFAULT GETDATE()
);

-- Bảng Import Details
CREATE TABLE Import_Details (
    id INT PRIMARY KEY IDENTITY(1,1),
    receipt_id INT FOREIGN KEY REFERENCES Import_Receipts(id),
    product_id INT FOREIGN KEY REFERENCES Products(id),
    quantity INT NOT NULL,
    import_price DECIMAL(15, 2) NOT NULL
);

-- Bảng Reviews (Đánh giá)
CREATE TABLE Reviews (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT FOREIGN KEY REFERENCES Users(id),
    product_id INT FOREIGN KEY REFERENCES Products(id),
    rating INT CHECK (rating >= 1 AND rating <= 5),
    comment NVARCHAR(MAX),
    is_approved BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);


-- Bảng Carts: Mỗi User chỉ cần 1 giỏ hàng (One-to-One)
CREATE TABLE Carts (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT UNIQUE FOREIGN KEY REFERENCES Users(id), -- UNIQUE để đảm bảo 1 người chỉ có 1 giỏ
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);

-- Bảng Cart_Items: Chi tiết các món trong giỏ
CREATE TABLE Cart_Items (
    id INT PRIMARY KEY IDENTITY(1,1),
    cart_id INT FOREIGN KEY REFERENCES Carts(id),
    product_id INT FOREIGN KEY REFERENCES Products(id),
    quantity INT DEFAULT 1 CHECK (quantity > 0), -- Số lượng phải > 0
    created_at DATETIME DEFAULT GETDATE(),
    
    -- Ràng buộc: Trong 1 giỏ, mỗi sản phẩm chỉ xuất hiện 1 dòng. 
    -- Nếu thêm trùng thì Update số lượng chứ không Insert dòng mới.
    CONSTRAINT UK_Cart_Product UNIQUE(cart_id, product_id)
);


CREATE TABLE Notifications (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT FOREIGN KEY REFERENCES Users(id), -- Thông báo của ai
    title NVARCHAR(255) NOT NULL, -- Tiêu đề (VD: Đơn hàng đã xác nhận)
    message NVARCHAR(MAX), -- Nội dung chi tiết
    is_read BIT DEFAULT 0, -- 0: Chưa đọc, 1: Đã đọc
    type VARCHAR(50) DEFAULT 'system', -- Phân loại: 'order', 'promotion', 'system'
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- 3. THÊM DỮ LIỆU MẪU (SEED DATA - 10 RECORD EACH)

-- Users (1 Admin, 9 Customers)
INSERT INTO Users (email, password, full_name, phone_number, address, role) VALUES 
('admin@store.com', 'hash_admin_123', N'Nguyễn Văn Quản Lý', '0909000001', N'Hà Nội', 'admin'),
('user1@gmail.com', 'hash_pass_1', N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'staff'),
('user2@gmail.com', 'hash_pass_2', N'Lê Văn Khách 2', '0912000002', N'Đà Nẵng', 'customer'),
('user3@gmail.com', 'hash_pass_3', N'Phạm Thị C', '0912000003', N'Cần Thơ', 'customer'),
('user4@gmail.com', 'hash_pass_4', N'Hoàng Văn D', '0912000004', N'Hải Phòng', 'customer'),
('user5@gmail.com', 'hash_pass_5', N'Vũ Thị E', '0912000005', N'Hà Nội', 'customer'),
('user6@gmail.com', 'hash_pass_6', N'Đặng Văn F', '0912000006', N'Nghệ An', 'customer'),
('user7@gmail.com', 'hash_pass_7', N'Bùi Thị G', '0912000007', N'Thanh Hóa', 'customer'),
('user8@gmail.com', 'hash_pass_8', N'Đỗ Văn H', '0912000008', N'Quảng Ninh', 'customer'),
('user9@gmail.com', 'hash_pass_9', N'Ngô Thị I', '0912000009', N'Huế', 'customer');

-- Categories
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

-- Brands
INSERT INTO Brands (name, origin) VALUES 
('Dell', 'USA'), ('Asus', 'Taiwan'), ('HP', 'USA'), ('Apple', 'USA'), ('Lenovo', 'China'),
('MSI', 'Taiwan'), ('Acer', 'Taiwan'), ('LG', 'Korea'), ('Gigabyte', 'Taiwan'), ('Razer', 'USA');

-- Products (10 sản phẩm đa dạng)
INSERT INTO Products (name, sku, category_id, brand_id, price, old_price, stock_quantity, cpu, ram, hard_drive, gpu, screen_size) VALUES 
(N'Dell XPS 13 9310', 'DEL-XPS-01', 2, 1, 25000000, 27000000, 10, 'i7 1165G7', '16GB', '512GB SSD', 'Iris Xe', '13.4 inch'),
(N'Asus ROG Strix G15', 'ASU-ROG-01', 1, 2, 32000000, 35000000, 5, 'Ryzen 7 6800H', '16GB', '1TB SSD', 'RTX 3060', '15.6 inch'),
(N'MacBook Air M1', 'APP-AIR-M1', 4, 4, 18000000, 20000000, 20, 'Apple M1', '8GB', '256GB SSD', '7-core GPU', '13.3 inch'),
(N'HP Spectre x360', 'HP-SPEC-01', 5, 3, 29000000, 31000000, 3, 'i7 1255U', '16GB', '1TB SSD', 'Iris Xe', '14 inch'),
(N'Lenovo Legion 5', 'LEN-LEG-05', 1, 5, 27500000, 30000000, 8, 'Ryzen 5 5600H', '16GB', '512GB SSD', 'RTX 3050Ti', '15.6 inch'),
(N'Acer Nitro 5', 'ACE-NIT-05', 1, 7, 19000000, 22000000, 15, 'i5 11400H', '8GB', '512GB SSD', 'GTX 1650', '15.6 inch'),
(N'MSI Modern 14', 'MSI-MOD-14', 6, 6, 14000000, 16000000, 12, 'i3 1115G4', '8GB', '256GB SSD', 'UHD Graphics', '14 inch'),
(N'LG Gram 17', 'LG-GRA-17', 2, 8, 35000000, 38000000, 4, 'i7 1260P', '16GB', '1TB SSD', 'Iris Xe', '17 inch'),
(N'Gigabyte G5', 'GIG-G5-01', 1, 9, 21000000, 24000000, 7, 'i5 11400H', '16GB', '512GB SSD', 'RTX 3050', '15.6 inch'),
(N'MacBook Pro 14 M1 Pro', 'APP-PRO-14', 4, 4, 45000000, 50000000, 6, 'M1 Pro', '16GB', '512GB SSD', '14-core GPU', '14.2 inch');

-- Product Images
INSERT INTO Product_Images (product_id, image_url, is_thumbnail) VALUES 
(1, '/images/dell-xps-1.jpg', 1), (1, '/images/dell-xps-2.jpg', 0),
(2, '/images/asus-rog-1.jpg', 1), (2, '/images/asus-rog-2.jpg', 0),
(3, '/images/mac-m1-1.jpg', 1), (4, '/images/hp-spectre-1.jpg', 1),
(5, '/images/lenovo-legion-1.jpg', 1), (6, '/images/acer-nitro-1.jpg', 1),
(7, '/images/msi-modern-1.jpg', 1), (8, '/images/lg-gram-1.jpg', 1);

-- Coupons
INSERT INTO Coupons (code, discount_type, discount_value, min_order_value, usage_limit, end_date) VALUES 
('SALE10', 'percentage', 10, 5000000, 100, '2025-12-31'),
('GIAM500K', 'fixed_amount', 500000, 10000000, 50, '2025-12-31'),
('TET2024', 'percentage', 15, 0, 200, '2025-02-15'),
('FREESHIP', 'fixed_amount', 30000, 2000000, 1000, '2025-12-31'),
('BLACKFRIDAY', 'percentage', 20, 0, 10, '2024-11-29'),
('STUDENT', 'percentage', 5, 0, 500, '2025-12-31'),
('NEWMEMBER', 'fixed_amount', 100000, 1000000, 1000, '2025-12-31'),
('SUMMERSALE', 'percentage', 8, 3000000, 100, '2025-08-30'),
('VIPCODE', 'fixed_amount', 1000000, 30000000, 5, '2025-12-31'),
('FLASHDEAL', 'percentage', 50, 0, 1, '2024-05-05');

-- Orders (Đơn hàng)
-- Lưu ý: total_money tự tính = subtotal + shipping - discount
INSERT INTO Orders (user_id, subtotal, shipping_fee, total_money, full_name, phone_number, address, status, payment_method, payment_status) VALUES 
(4, 25000000, 0, 25000000, N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'completed', 'vietqr', 'paid'),
(3, 32000000, 50000, 32050000, N'Lê Văn Khách 2', '0912000002', N'Đà Nẵng', 'pending', 'cod', 'unpaid'),
(NULL, 18000000, 100000, 18100000, N'Nguyễn Văn Vãng Lai', '0988888888', N'Hà Giang', 'shipping', 'cod', 'unpaid'), -- Khách vãng lai
(4, 29000000, 0, 28000000, N'Phạm Thị C', '0912000003', N'Cần Thơ', 'completed', 'vietqr', 'paid'),
(5, 27500000, 0, 27500000, N'Hoàng Văn D', '0912000004', N'Hải Phòng', 'cancelled', 'cod', 'unpaid'),
(6, 19000000, 30000, 18530000, N'Đặng Văn F', '0912000006', N'Nghệ An', 'confirmed', 'vietqr', 'paid'),
(7, 14000000, 0, 14000000, N'Bùi Thị G', '0912000007', N'Thanh Hóa', 'pending', 'cod', 'unpaid'),
(5, 35000000, 0, 35000000, N'Trần Thị Khách 1', '0912000001', N'Hồ Chí Minh', 'pending', 'vietqr', 'unpaid'), -- User 2 mua lần 2
(8, 21000000, 50000, 21050000, N'Đỗ Văn H', '0912000008', N'Quảng Ninh', 'shipping', 'cod', 'unpaid'),
(9, 45000000, 0, 45000000, N'Ngô Thị I', '0912000009', N'Huế', 'completed', 'vnpay', 'paid');

-- Order Details
INSERT INTO Order_Details (order_id, product_id, quantity, price) VALUES 
(1, 1, 1, 25000000), -- Đơn 1 mua Dell XPS
(2, 2, 1, 32000000), -- Đơn 2 mua ROG
(3, 3, 1, 18000000), -- Đơn 3 (vãng lai) mua Mac Air
(4, 4, 1, 29000000), 
(5, 5, 1, 27500000),
(6, 6, 1, 19000000),
(7, 7, 1, 14000000),
(8, 8, 1, 35000000),
(9, 9, 1, 21000000),
(10, 10, 1, 45000000);

-- Import Receipts (Phiếu nhập kho)
INSERT INTO Import_Receipts (admin_id, supplier_name, total_cost) VALUES 
(2, N'FPT Trading', 500000000),
(2, N'Digiworld', 300000000),
(2, N'Viễn Sơn', 150000000),
(2, N'Petrosetco', 200000000),
(2, N'FPT Trading', 100000000),
(2, N'Synnex FPT', 400000000),
(2, N'Nhà Phân Phối A', 50000000),
(2, N'Nhà Phân Phối B', 80000000),
(2, N'Samsung Vina', 120000000),
(2, N'LG VN', 150000000);

-- Import Details
INSERT INTO Import_Details (receipt_id, product_id, quantity, import_price) VALUES 
(1, 1, 10, 20000000), -- Nhập Dell XPS giá vốn 20tr
(1, 2, 5, 28000000),
(2, 3, 20, 15000000),
(3, 4, 3, 25000000),
(4, 5, 10, 24000000),
(5, 6, 15, 16000000),
(6, 7, 20, 11000000),
(7, 8, 5, 30000000),
(8, 9, 10, 18000000),
(9, 10, 5, 38000000);

-- Reviews
INSERT INTO Reviews (user_id, product_id, rating, comment, is_approved) VALUES 
(4, 1, 5, N'Máy rất đẹp, mỏng nhẹ, đáng tiền!', 1),
(3, 2, 4, N'Máy mạnh nhưng quạt hơi ồn khi chơi game nặng.', 1),
(4, 3, 5, N'Pin trâu dã man, dùng cả ngày không hết.', 1),
(5, 4, 5, N'Màn hình cảm ứng mượt, xoay gập tiện lợi.', 1),
(6, 5, 3, N'Máy hơi nóng, cần mua thêm đế tản nhiệt.', 1),
(7, 6, 4, N'Giá rẻ cấu hình ngon, sinh viên nên mua.', 1),
(8, 7, 5, N'Rất nhẹ, mang đi học tiện.', 1),
(9, 8, 5, N'Màn 17 inch to đã mắt nhưng máy vẫn nhẹ.', 1),
(7, 9, 4, N'Hiệu năng tốt trong tầm giá.', 1),
(3, 10, 5, N'Đỉnh cao của MacBook, màn hình quá đẹp.', 1);


-- --- DỮ LIỆU MẪU CHO CART ---

-- 1. Tạo giỏ hàng cho User 2 và User 3 (đã login)
INSERT INTO Carts (user_id) VALUES (5), (3), (4);

-- 2. Thêm sản phẩm vào giỏ hàng
-- Giỏ của User 2: Đang để sẵn 1 con Dell XPS và 2 con chuột
INSERT INTO Cart_Items (cart_id, product_id, quantity) VALUES 
(1, 1, 1), -- Dell XPS
(1, 9, 2); -- Gigabyte G5 (VD)

-- Giỏ của User 3: Đang để 1 con Mac M1
INSERT INTO Cart_Items (cart_id, product_id, quantity) VALUES 
(2, 3, 1);


-- --- DỮ LIỆU MẪU CHO NOTIFICATIONS ---

INSERT INTO Notifications (user_id, title, message, type, is_read, created_at) VALUES 
(2, N'Đặt hàng thành công', N'Đơn hàng #1 của bạn đã được ghi nhận và đang chờ xử lý.', 'order', 1, GETDATE()),
(2, N'Đang giao hàng', N'Đơn hàng #1 đang được giao bởi Shipper Nguyễn Văn A.', 'order', 0, GETDATE()),
(3, N'Khuyến mãi Tết', N'Nhập mã TET2024 để được giảm giá 15% cho Laptop Gaming.', 'promotion', 0, GETDATE()),
(2, N'Cảnh báo bảo mật', N'Có thiết bị lạ vừa đăng nhập vào tài khoản của bạn.', 'system', 0, GETDATE()),
(4, N'Hoàn tiền thành công', N'Yêu cầu hoàn tiền cho đơn hàng #5 đã được chấp nhận.', 'order', 1, GETDATE());

GO
GO