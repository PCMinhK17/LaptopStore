using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopStore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old constraint and add new one to allow 'pending' status
            migrationBuilder.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK__Users__status__3C69FB99;");
            migrationBuilder.Sql("ALTER TABLE Users ADD CONSTRAINT CK__Users__status__3C69FB99 CHECK (status IN ('active', 'banned', 'pending'));");

            // Create Email_Verification_Tokens table if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Email_Verification_Tokens]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Email_Verification_Tokens] (
                        [id] INT PRIMARY KEY IDENTITY(1,1),
                        [user_id] INT NOT NULL,
                        [token] VARCHAR(100) NOT NULL,
                        [created_at] DATETIME DEFAULT GETDATE(),
                        [expires_at] DATETIME NOT NULL,
                        [is_used] BIT DEFAULT 0,
                        CONSTRAINT [FK_EmailVerificationTokens_Users] FOREIGN KEY ([user_id]) REFERENCES [Users]([id]) ON DELETE CASCADE
                    )
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[Email_Verification_Tokens];");
            migrationBuilder.Sql("ALTER TABLE Users DROP CONSTRAINT IF EXISTS CK__Users__status__3C69FB99;");
            migrationBuilder.Sql("ALTER TABLE Users ADD CONSTRAINT CK__Users__status__3C69FB99 CHECK (status IN ('active', 'banned'));");
        }
    }
}
