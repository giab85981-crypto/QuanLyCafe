using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class QrGuestOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrOrderRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestKey = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IdTable = table.Column<int>(type: "int", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IdBill = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrOrderRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QrOrderRequest_Bill_IdBill",
                        column: x => x.IdBill,
                        principalTable: "Bill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrOrderRequest_TableFood_IdTable",
                        column: x => x.IdTable,
                        principalTable: "TableFood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrOrderRequest_IdBill",
                table: "QrOrderRequest",
                column: "IdBill");

            migrationBuilder.CreateIndex(
                name: "IX_QrOrderRequest_IdTable",
                table: "QrOrderRequest",
                column: "IdTable");

            migrationBuilder.CreateIndex(
                name: "IX_QrOrderRequest_RequestKey",
                table: "QrOrderRequest",
                column: "RequestKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrOrderRequest_Status_CreatedAt",
                table: "QrOrderRequest",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrOrderRequest");
        }
    }
}
