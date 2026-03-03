-- Script thêm cột avatar_url vào bảng Users
-- Chạy script này trong SQL Server Management Studio hoặc công cụ quản lý database của bạn

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'avatar_url')
BEGIN
    ALTER TABLE Users ADD avatar_url NVARCHAR(255) NULL;
    PRINT 'Cột avatar_url đã được thêm vào bảng Users thành công!';
END
ELSE
BEGIN
    PRINT 'Cột avatar_url đã tồn tại trong bảng Users.';
END
