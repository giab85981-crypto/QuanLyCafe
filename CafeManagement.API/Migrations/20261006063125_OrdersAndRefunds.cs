using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class OrdersAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill");

            migrationBuilder.DropIndex(
                name: "IX_CashEntry_IdBill",
                table: "CashEntry");

            migrationBuilder.AddColumn<string>(
                name: "FoodNameSnapshot",
                table: "BillInfo",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "IdTable",
                table: "Bill",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Bill",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Bill",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "Bill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Bill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreationKey",
                table: "Bill",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Bill",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "Bill",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaidBy",
                table: "Bill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Bill",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Bill",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundMethod",
                table: "Bill",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TableNameSnapshot",
                table: "Bill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE b SET OrderType = 'DineIn', PaymentMethod = COALESCE(c.PaymentMethod, 'Unknown'),
    TableNameSnapshot = t.Name, PaidBy = COALESCE(c.CreatedBy, '')
FROM Bill b JOIN TableFood t ON t.Id = b.IdTable
LEFT JOIN CashEntry c ON c.IdBill = b.Id AND c.Direction = 'In';
UPDATE i SET FoodNameSnapshot = f.Name FROM BillInfo i JOIN Food f ON f.Id = i.IdFood;");

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_IdBill_Direction",
                table: "CashEntry",
                columns: new[] { "IdBill", "Direction" },
                unique: true,
                filter: "[IdBill] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_CreationKey",
                table: "Bill",
                column: "CreationKey",
                unique: true,
                filter: "[CreationKey] <> ''");

            migrationBuilder.AddForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill",
                column: "IdTable",
                principalTable: "TableFood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill");

            migrationBuilder.DropIndex(
                name: "IX_CashEntry_IdBill_Direction",
                table: "CashEntry");

            migrationBuilder.DropIndex(
                name: "IX_Bill_CreationKey",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "FoodNameSnapshot",
                table: "BillInfo");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "CreationKey",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "PaidBy",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "RefundMethod",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "TableNameSnapshot",
                table: "Bill");

            migrationBuilder.AlterColumn<int>(
                name: "IdTable",
                table: "Bill",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashEntry_IdBill",
                table: "CashEntry",
                column: "IdBill",
                unique: true,
                filter: "[IdBill] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill",
                column: "IdTable",
                principalTable: "TableFood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
