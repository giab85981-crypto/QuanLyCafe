using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseLotsAndCashbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Supplier",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StockMovement",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdDocument",
                table: "StockMovement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdImportReceipt",
                table: "StockMovement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdLot",
                table: "StockMovement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "StockMovement",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Ingredient",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdGroup",
                table: "Ingredient",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Ingredient",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPaymentTracking",
                table: "ImportReceipt",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ImportReceipt",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestKey",
                table: "ImportReceipt",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ConversionFactor",
                table: "ImportDetail",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IdLot",
                table: "ImportDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InputQuantity",
                table: "ImportDetail",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InputUnitPrice",
                table: "ImportDetail",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "ImportDetail",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CashEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdImportReceipt = table.Column<int>(type: "int", nullable: true),
                    IdBill = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashEntry_Bill_IdBill",
                        column: x => x.IdBill,
                        principalTable: "Bill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashEntry_ImportReceipt_IdImportReceipt",
                        column: x => x.IdImportReceipt,
                        principalTable: "ImportReceipt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngredientGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientUnit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdIngredient = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientUnit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientUnit_Ingredient_IdIngredient",
                        column: x => x.IdIngredient,
                        principalTable: "Ingredient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdIngredient = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdImportReceipt = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLot_ImportReceipt_IdImportReceipt",
                        column: x => x.IdImportReceipt,
                        principalTable: "ImportReceipt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLot_Ingredient_IdIngredient",
                        column: x => x.IdIngredient,
                        principalTable: "Ingredient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseDocumentLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDocument = table.Column<int>(type: "int", nullable: false),
                    IdIngredient = table.Column<int>(type: "int", nullable: false),
                    BeforeQuantity = table.Column<double>(type: "float", nullable: false),
                    AfterQuantity = table.Column<double>(type: "float", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseDocumentLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseDocumentLine_Ingredient_IdIngredient",
                        column: x => x.IdIngredient,
                        principalTable: "Ingredient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseDocumentLine_WarehouseDocument_IdDocument",
                        column: x => x.IdDocument,
                        principalTable: "WarehouseDocument",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IdDocument",
                table: "StockMovement",
                column: "IdDocument");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IdImportReceipt",
                table: "StockMovement",
                column: "IdImportReceipt");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_IdLot",
                table: "StockMovement",
                column: "IdLot");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredient_Code",
                table: "Ingredient",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredient_IdGroup",
                table: "Ingredient",
                column: "IdGroup");

            migrationBuilder.CreateIndex(
                name: "IX_ImportReceipt_RequestKey",
                table: "ImportReceipt",
                column: "RequestKey",
                unique: true,
                filter: "[RequestKey] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ImportDetail_IdLot",
                table: "ImportDetail",
                column: "IdLot");

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_IdBill",
                table: "CashEntry",
                column: "IdBill",
                unique: true,
                filter: "[IdBill] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_IdImportReceipt",
                table: "CashEntry",
                column: "IdImportReceipt");

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_RequestKey",
                table: "CashEntry",
                column: "RequestKey",
                unique: true,
                filter: "[RequestKey] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientGroup_Name",
                table: "IngredientGroup",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredientUnit_IdIngredient_Name",
                table: "IngredientUnit",
                columns: new[] { "IdIngredient", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_IdImportReceipt",
                table: "StockLot",
                column: "IdImportReceipt");

            migrationBuilder.CreateIndex(
                name: "IX_StockLot_IdIngredient",
                table: "StockLot",
                column: "IdIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDocument_RequestKey",
                table: "WarehouseDocument",
                column: "RequestKey",
                unique: true,
                filter: "[RequestKey] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDocumentLine_IdDocument",
                table: "WarehouseDocumentLine",
                column: "IdDocument");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseDocumentLine_IdIngredient",
                table: "WarehouseDocumentLine",
                column: "IdIngredient");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetail_StockLot_IdLot",
                table: "ImportDetail",
                column: "IdLot",
                principalTable: "StockLot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredient_IngredientGroup_IdGroup",
                table: "Ingredient",
                column: "IdGroup",
                principalTable: "IngredientGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovement_ImportReceipt_IdImportReceipt",
                table: "StockMovement",
                column: "IdImportReceipt",
                principalTable: "ImportReceipt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovement_StockLot_IdLot",
                table: "StockMovement",
                column: "IdLot",
                principalTable: "StockLot",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovement_WarehouseDocument_IdDocument",
                table: "StockMovement",
                column: "IdDocument",
                principalTable: "WarehouseDocument",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(@"INSERT INTO IngredientGroup(Name) VALUES (N'Nguyên liệu');
UPDATE Ingredient SET Code=CONCAT('NL',Id), IdGroup=(SELECT TOP 1 Id FROM IngredientGroup ORDER BY Id);
INSERT INTO StockLot(IdIngredient,Code,Quantity,UnitCost,CreatedAt) SELECT Id,N'TON-DAU-CU',Quantity,UnitCost,GETDATE() FROM Ingredient WHERE Quantity>0;");        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetail_StockLot_IdLot",
                table: "ImportDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_Ingredient_IngredientGroup_IdGroup",
                table: "Ingredient");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovement_ImportReceipt_IdImportReceipt",
                table: "StockMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovement_StockLot_IdLot",
                table: "StockMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovement_WarehouseDocument_IdDocument",
                table: "StockMovement");

            migrationBuilder.DropTable(
                name: "CashEntry");

            migrationBuilder.DropTable(
                name: "IngredientGroup");

            migrationBuilder.DropTable(
                name: "IngredientUnit");

            migrationBuilder.DropTable(
                name: "StockLot");

            migrationBuilder.DropTable(
                name: "WarehouseDocumentLine");

            migrationBuilder.DropTable(
                name: "WarehouseDocument");

            migrationBuilder.DropIndex(
                name: "IX_StockMovement_IdDocument",
                table: "StockMovement");

            migrationBuilder.DropIndex(
                name: "IX_StockMovement_IdImportReceipt",
                table: "StockMovement");

            migrationBuilder.DropIndex(
                name: "IX_StockMovement_IdLot",
                table: "StockMovement");

            migrationBuilder.DropIndex(
                name: "IX_Ingredient_Code",
                table: "Ingredient");

            migrationBuilder.DropIndex(
                name: "IX_Ingredient_IdGroup",
                table: "Ingredient");

            migrationBuilder.DropIndex(
                name: "IX_ImportReceipt_RequestKey",
                table: "ImportReceipt");

            migrationBuilder.DropIndex(
                name: "IX_ImportDetail_IdLot",
                table: "ImportDetail");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "IdDocument",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "IdImportReceipt",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "IdLot",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "StockMovement");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Ingredient");

            migrationBuilder.DropColumn(
                name: "IdGroup",
                table: "Ingredient");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Ingredient");

            migrationBuilder.DropColumn(
                name: "HasPaymentTracking",
                table: "ImportReceipt");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ImportReceipt");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                table: "ImportReceipt");

            migrationBuilder.DropColumn(
                name: "ConversionFactor",
                table: "ImportDetail");

            migrationBuilder.DropColumn(
                name: "IdLot",
                table: "ImportDetail");

            migrationBuilder.DropColumn(
                name: "InputQuantity",
                table: "ImportDetail");

            migrationBuilder.DropColumn(
                name: "InputUnitPrice",
                table: "ImportDetail");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "ImportDetail");
        }
    }
}

