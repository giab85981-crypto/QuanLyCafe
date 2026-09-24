using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCostPriceToFoodAndBillInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Foods",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "BillInfos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "BillInfos");
        }
    }
}
