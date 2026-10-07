using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class CashierShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdShift",
                table: "CashEntry",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdShift",
                table: "Bill",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashierShift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CountedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OpeningNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClosingNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClosingSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierShift", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierShift_Account_UserName",
                        column: x => x.UserName,
                        principalTable: "Account",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_IdShift",
                table: "CashEntry",
                column: "IdShift");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_IdShift",
                table: "Bill",
                column: "IdShift");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_RequestKey",
                table: "CashierShift",
                column: "RequestKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_UserName",
                table: "CashierShift",
                column: "UserName",
                unique: true,
                filter: "[ClosedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Bill_CashierShift_IdShift",
                table: "Bill",
                column: "IdShift",
                principalTable: "CashierShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashEntry_CashierShift_IdShift",
                table: "CashEntry",
                column: "IdShift",
                principalTable: "CashierShift",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bill_CashierShift_IdShift",
                table: "Bill");

            migrationBuilder.DropForeignKey(
                name: "FK_CashEntry_CashierShift_IdShift",
                table: "CashEntry");

            migrationBuilder.DropTable(
                name: "CashierShift");

            migrationBuilder.DropIndex(
                name: "IX_CashEntry_IdShift",
                table: "CashEntry");

            migrationBuilder.DropIndex(
                name: "IX_Bill_IdShift",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "IdShift",
                table: "CashEntry");

            migrationBuilder.DropColumn(
                name: "IdShift",
                table: "Bill");
        }
    }
}
