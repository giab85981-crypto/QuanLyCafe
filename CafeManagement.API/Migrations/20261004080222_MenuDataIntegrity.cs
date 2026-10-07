using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class MenuDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "KitchenOrderDetail",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"UPDATE Food SET MenuKind=N'Khác' WHERE MenuKind=N'KhÃ¡c';
UPDATE Food SET ItemType=N'Món chế biến' WHERE ItemType=N'MÃ³n cháº¿ biáº¿n';
UPDATE Food SET ItemType=N'Hàng hóa' WHERE ItemType=N'HÃ ng hÃ³a';
UPDATE Food SET ItemType=N'Dịch vụ' WHERE ItemType=N'Dá»‹ch vá»¥';
UPDATE ItemType SET Name=N'Món chế biến' WHERE Name=N'MÃ³n cháº¿ biáº¿n' AND NOT EXISTS(SELECT 1 FROM ItemType WHERE Name=N'Món chế biến');
UPDATE ItemType SET Name=N'Hàng hóa' WHERE Name=N'HÃ ng hÃ³a' AND NOT EXISTS(SELECT 1 FROM ItemType WHERE Name=N'Hàng hóa');
UPDATE ItemType SET Name=N'Dịch vụ' WHERE Name=N'Dá»‹ch vá»¥' AND NOT EXISTS(SELECT 1 FROM ItemType WHERE Name=N'Dịch vụ');
DELETE FROM ItemType WHERE Name IN(N'MÃ³n cháº¿ biáº¿n', N'HÃ ng hÃ³a', N'Dá»‹ch vá»¥') AND NOT EXISTS(SELECT 1 FROM Food WHERE Food.ItemType=ItemType.Name);
UPDATE i SET IngredientsJson=(SELECT r.IdIngredient, r.Amount FROM Recipe r WHERE r.IdFood=i.IdFood AND r.IdVariant IS NULL FOR JSON PATH), UnitPrice=ISNULL(i.UnitPrice,f.Price) FROM BillInfo i JOIN Bill b ON b.Id=i.IdBill JOIN Food f ON f.Id=i.IdFood WHERE b.Status=0 AND i.SentCount=0;");            migrationBuilder.CreateIndex(
                name: "IX_Food_Code",
                table: "Food",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Food_Code",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "KitchenOrderDetail");
        }
    }
}

