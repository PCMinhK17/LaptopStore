using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopStore.Migrations
{
    /// <inheritdoc />
    public partial class FixPhoneNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old unique index/constraint on phone_number if exists, then recreate as filtered
            migrationBuilder.Sql(@"
                -- Drop unique constraint or index on phone_number (try both forms)
                DECLARE @constraintName NVARCHAR(256);
                
                -- Check for unique constraint
                SELECT @constraintName = tc.CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                WHERE tc.TABLE_NAME = 'Users' AND ccu.COLUMN_NAME = 'phone_number' AND tc.CONSTRAINT_TYPE = 'UNIQUE';
                
                IF @constraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [Users] DROP CONSTRAINT [' + @constraintName + ']');
                END

                -- Check for unique index
                DECLARE @indexName NVARCHAR(256);
                SELECT @indexName = i.name
                FROM sys.indexes i
                JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.object_id = OBJECT_ID('Users') AND c.name = 'phone_number' AND i.is_unique = 1;

                IF @indexName IS NOT NULL
                BEGIN
                    EXEC('DROP INDEX [' + @indexName + '] ON [Users]');
                END

                -- Create filtered unique index (allows multiple NULLs)
                CREATE UNIQUE INDEX [UQ__Users__A1936A6BB8BC7BA3] 
                ON [Users] ([phone_number]) 
                WHERE [phone_number] IS NOT NULL;
            ");

            // Add ban_reason column if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ban_reason')
                BEGIN
                    ALTER TABLE [Users] ADD [ban_reason] NVARCHAR(500) NULL;
                END
            ");

            // Alter comment column max length
            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "Reviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert filtered index back to regular unique index
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Users') AND name = 'UQ__Users__A1936A6BB8BC7BA3')
                BEGIN
                    DROP INDEX [UQ__Users__A1936A6BB8BC7BA3] ON [Users];
                END

                CREATE UNIQUE INDEX [UQ__Users__A1936A6BB8BC7BA3] ON [Users] ([phone_number]);
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'ban_reason')
                BEGIN
                    ALTER TABLE [Users] DROP COLUMN [ban_reason];
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
