using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class MenuOptionsAndKitchenStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdVariant",
                table: "Recipe",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledCount",
                table: "KitchenOrderDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdBillInfo",
                table: "KitchenOrderDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionLabel",
                table: "KitchenOrderDetail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "Ingredient",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Food",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Food",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Food",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Food",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Food",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTopping",
                table: "Food",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IdVariant",
                table: "BillInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IngredientsJson",
                table: "BillInfo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionLabel",
                table: "BillInfo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "BillInfo",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SentCount",
                table: "BillInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FoodTopping",
                columns: table => new
                {
                    IdFood = table.Column<int>(type: "int", nullable: false),
                    IdTopping = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodTopping", x => new { x.IdFood, x.IdTopping });
                    table.ForeignKey(
                        name: "FK_FoodTopping_Food_IdFood",
                        column: x => x.IdFood,
                        principalTable: "Food",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FoodTopping_Food_IdTopping",
                        column: x => x.IdTopping,
                        principalTable: "Food",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FoodVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdFood = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPrice = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodVariant_Food_IdFood",
                        column: x => x.IdFood,
                        principalTable: "Food",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdIngredient = table.Column<int>(type: "int", nullable: false),
                    IdKitchenDetail = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovement_Ingredient_IdIngredient",
                        column: x => x.IdIngredient,
                        principalTable: "Ingredient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovement_KitchenOrderDetail_IdKitchenDetail",
                        column: x => x.IdKitchenDetail,
                        principalTable: "KitchenOrderDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Recipe_IdVariant",
                table: "Recipe",
                column: "IdVariant");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenOrderDetail_IdBillInfo",
                table: "KitchenOrderDetail",
                column: "IdBillInfo");

            migrationBuilder.CreateIndex(
                name: "IX_BillInfo_IdVariant",
                table: "BillInfo",
                column: "IdVariant");

            migrationBuilder.CreateIndex(
                name: "IX_FoodTopping_IdTopping",
                table: "FoodTopping",
                column: "IdTopping");

            migrationBuilder.CreateIndex(
                name: "IX_FoodVariant_IdFood",
                table: "FoodVariant",
                column: "IdFood");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IdIngredient",
                table: "StockMovement",
                column: "IdIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IdKitchenDetail",
                table: "StockMovement",
                column: "IdKitchenDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_BillInfo_FoodVariant_IdVariant",
                table: "BillInfo",
                column: "IdVariant",
                principalTable: "FoodVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderDetail_BillInfo_IdBillInfo",
                table: "KitchenOrderDetail",
                column: "IdBillInfo",
                principalTable: "BillInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipe_FoodVariant_IdVariant",
                table: "Recipe",
                column: "IdVariant",
                principalTable: "FoodVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(@"UPDATE Food SET Code = CONCAT('MON', Id);
UPDATE BillInfo SET IngredientsJson = '[]', OptionsJson = '[]';
UPDATE d SET IdBillInfo = i.Id FROM KitchenOrderDetail d JOIN KitchenOrder k ON k.Id=d.IdKitchenOrder JOIN BillInfo i ON i.IdBill=k.IdBill AND i.IdFood=d.IdFood WHERE (SELECT COUNT(*) FROM BillInfo b WHERE b.IdBill=i.IdBill AND b.IdFood=i.IdFood)=1;
UPDATE i SET SentCount = CASE WHEN b.Status=1 THEN i.Count ELSE CASE WHEN ISNULL(s.Qty,0)>i.Count THEN i.Count ELSE ISNULL(s.Qty,0) END END FROM BillInfo i JOIN Bill b ON b.Id=i.IdBill OUTER APPLY (SELECT SUM(d.Count) Qty FROM KitchenOrderDetail d WHERE d.IdBillInfo=i.Id) s;");        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillInfo_FoodVariant_IdVariant",
                table: "BillInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderDetail_BillInfo_IdBillInfo",
                table: "KitchenOrderDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipe_FoodVariant_IdVariant",
                table: "Recipe");

            migrationBuilder.DropTable(
                name: "FoodTopping");

            migrationBuilder.DropTable(
                name: "FoodVariant");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropIndex(
                name: "IX_Recipe_IdVariant",
                table: "Recipe");

            migrationBuilder.DropIndex(
                name: "IX_KitchenOrderDetail_IdBillInfo",
                table: "KitchenOrderDetail");

            migrationBuilder.DropIndex(
                name: "IX_BillInfo_IdVariant",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "IdVariant",
                table: "Recipe");

            migrationBuilder.DropColumn(
                name: "CancelledCount",
                table: "KitchenOrderDetail");

            migrationBuilder.DropColumn(
                name: "IdBillInfo",
                table: "KitchenOrderDetail");

            migrationBuilder.DropColumn(
                name: "OptionLabel",
                table: "KitchenOrderDetail");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "Ingredient");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "IsTopping",
                table: "Food");

            migrationBuilder.DropColumn(
                name: "IdVariant",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "IngredientsJson",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "OptionLabel",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "SentCount",
                table: "BillInfo");
        }
    }
}

