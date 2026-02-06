USE LaptopStoreDB;
GO

-- 1. Xóa Check Constraint gây lỗi (Dựa trên log lỗi của bạn)
IF OBJECT_ID('CK__Users__status__3C69FB99', 'C') IS NOT NULL
BEGIN
    ALTER TABLE Users DROP CONSTRAINT CK__Users__status__3C69FB99;
    PRINT 'Da xoa constraint CK__Users__status__3C69FB99';
END

-- Xóa constraint cũ nếu script trước đó đã tạo (để chắc chắn)
IF OBJECT_ID('CK_Users_Status', 'C') IS NOT NULL
BEGIN
    ALTER TABLE Users DROP CONSTRAINT CK_Users_Status;
    PRINT 'Da xoa constraint CK_Users_Status';
END

-- Xóa constraint mặc định khác nếu có (quét lại một lần nữa cho chắc)
DECLARE @ConstraintName NVARCHAR(255)
SELECT TOP 1 @ConstraintName = name 
FROM sys.check_constraints 
WHERE parent_object_id = OBJECT_ID('Users') 
AND definition LIKE '%status%'

IF @ConstraintName IS NOT NULL
BEGIN
    DECLARE @SQL NVARCHAR(MAX) = 'ALTER TABLE Users DROP CONSTRAINT ' + @ConstraintName
    EXEC sp_executesql @SQL
    PRINT 'Da xoa constraint con sot lai: ' + @ConstraintName
END
GO

-- 2. Thêm lại Constraint mới chuẩn xác
ALTER TABLE Users
ADD CONSTRAINT CK_Users_Status 
CHECK (status IN ('active', 'banned', 'pending'));
PRINT 'Da them constraint moi: CK_Users_Status (cho phep pending)'
GO

-- 3. Kiểm tra lại bảng Token
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Email_Verification_Tokens' AND xtype='U')
BEGIN
    CREATE TABLE Email_Verification_Tokens (
        id INT PRIMARY KEY IDENTITY(1,1),
        user_id INT NOT NULL,
        token VARCHAR(100) NOT NULL,
        created_at DATETIME DEFAULT GETDATE(),
        expires_at DATETIME NOT NULL,
        is_used BIT DEFAULT 0,
        
        CONSTRAINT FK_EmailVerificationTokens_Users 
        FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE
    );

    CREATE INDEX IX_EmailVerificationTokens_Token ON Email_Verification_Tokens(token);
    CREATE INDEX IX_EmailVerificationTokens_UserId ON Email_Verification_Tokens(user_id);
    
    PRINT 'Tao bang Email_Verification_Tokens thanh cong'
END
ELSE
BEGIN
    PRINT 'Bang Email_Verification_Tokens da ton tai'
END
GO
