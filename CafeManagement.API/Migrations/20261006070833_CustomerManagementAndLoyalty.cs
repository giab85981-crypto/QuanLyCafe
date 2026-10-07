using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class CustomerManagementAndLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Customer",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "Customer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Customer",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Customer",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customer",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Customer",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "IdGroup",
                table: "Customer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNameSnapshot",
                table: "Bill",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneSnapshot",
                table: "Bill",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "HasRecordedPayment",
                table: "Bill",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PointDiscount",
                table: "Bill",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PointsEarned",
                table: "Bill",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                table: "Bill",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CustomerGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPointEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCustomer = table.Column<int>(type: "int", nullable: false),
                    IdBill = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPointEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPointEntry_Bill_IdBill",
                        column: x => x.IdBill,
                        principalTable: "Bill",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPointEntry_Customer_IdCustomer",
                        column: x => x.IdCustomer,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
UPDATE c SET Code = 'KH' + RIGHT('000000' + CAST(c.Id AS varchar(20)), CASE WHEN c.Id > 999999 THEN LEN(CAST(c.Id AS varchar(20))) ELSE 6 END),
    IsActive = 1, CreatedAt = COALESCE((SELECT MIN(b.DateCheckIn) FROM Bill b WHERE b.IdCustomer = c.Id), GETDATE()) FROM Customer c;
UPDATE b SET CustomerNameSnapshot = c.Name, CustomerPhoneSnapshot = c.Phone FROM Bill b JOIN Customer c ON b.IdCustomer = c.Id WHERE b.Status IN (1,3);
UPDATE Bill SET HasRecordedPayment = 1 WHERE Status IN (1,3) AND (TotalPrice > 0 OR Discount = 100);
INSERT INTO CustomerGroup (Name) VALUES (N'Khách thường'), (N'VIP');
");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Code",
                table: "Customer",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_IdGroup",
                table: "Customer",
                column: "IdGroup");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Phone",
                table: "Customer",
                column: "Phone",
                unique: true,
                filter: "[Phone] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroup_Name",
                table: "CustomerGroup",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPointEntry_IdBill_Kind",
                table: "CustomerPointEntry",
                columns: new[] { "IdBill", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPointEntry_IdCustomer",
                table: "CustomerPointEntry",
                column: "IdCustomer");

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_CustomerGroup_IdGroup",
                table: "Customer",
                column: "IdGroup",
                principalTable: "CustomerGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customer_CustomerGroup_IdGroup",
                table: "Customer");

            migrationBuilder.DropTable(
                name: "CustomerGroup");

            migrationBuilder.DropTable(
                name: "CustomerPointEntry");

            migrationBuilder.DropIndex(
                name: "IX_Customer_Code",
                table: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_Customer_IdGroup",
                table: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_Customer_Phone",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IdGroup",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "CustomerNameSnapshot",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneSnapshot",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "HasRecordedPayment",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "PointDiscount",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "Bill");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                table: "Bill");
        }
    }
}
