using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace CafeManagement.API.Migrations;

public partial class DashboardSalesTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "GuestCount", table: "Bill", type: "int", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "UnitPrice", table: "BillInfo", type: "decimal(18,2)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "MenuKind", table: "Food", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Khác");
        migrationBuilder.CreateTable(name: "ItemType", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ItemType", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_ItemType_Name", table: "ItemType", column: "Name", unique: true);
        migrationBuilder.Sql("INSERT INTO ItemType (Name) SELECT DISTINCT ItemType FROM Food WHERE LTRIM(RTRIM(ItemType)) <> ''");
        migrationBuilder.Sql("INSERT INTO ItemType (Name) SELECT Name FROM (VALUES (N'Món chế biến'), (N'Hàng hóa'), (N'Dịch vụ')) AS Defaults(Name) WHERE NOT EXISTS (SELECT 1 FROM ItemType t WHERE t.Name = Defaults.Name)");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ItemType");
        migrationBuilder.DropColumn(name: "GuestCount", table: "Bill");
        migrationBuilder.DropColumn(name: "UnitPrice", table: "BillInfo");
        migrationBuilder.DropColumn(name: "MenuKind", table: "Food");
    }
}