using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class FixSingular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Roles_IdRole",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_BillInfos_Bills_IdBill",
                table: "BillInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_BillInfos_Foods_IdFood",
                table: "BillInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Customers_IdCustomer",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_Bills_TableFoods_IdTable",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_Foods_FoodCategories_IdCategory",
                table: "Foods");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetails_ImportReceipts_IdImportReceipt",
                table: "ImportDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetails_Ingredients_IdIngredient",
                table: "ImportDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Accounts_UserName",
                table: "ImportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipts_Suppliers_IdSupplier",
                table: "ImportReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderDetails_Foods_IdFood",
                table: "KitchenOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderDetails_KitchenOrders_IdKitchenOrder",
                table: "KitchenOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrders_Bills_IdBill",
                table: "KitchenOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Foods_IdFood",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Ingredients_IdIngredient",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_IdPermission",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_IdRole",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TableFoods",
                table: "TableFoods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recipes",
                table: "Recipes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenOrders",
                table: "KitchenOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenOrderDetails",
                table: "KitchenOrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ingredients",
                table: "Ingredients");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceipts",
                table: "ImportReceipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportDetails",
                table: "ImportDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Foods",
                table: "Foods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FoodCategories",
                table: "FoodCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bills",
                table: "Bills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillInfos",
                table: "BillInfos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts");

            migrationBuilder.RenameTable(
                name: "TableFoods",
                newName: "TableFood");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "Supplier");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Role");

            migrationBuilder.RenameTable(
                name: "RolePermissions",
                newName: "RolePermission");

            migrationBuilder.RenameTable(
                name: "Recipes",
                newName: "Recipe");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "Permission");

            migrationBuilder.RenameTable(
                name: "KitchenOrders",
                newName: "KitchenOrder");

            migrationBuilder.RenameTable(
                name: "KitchenOrderDetails",
                newName: "KitchenOrderDetail");

            migrationBuilder.RenameTable(
                name: "Ingredients",
                newName: "Ingredient");

            migrationBuilder.RenameTable(
                name: "ImportReceipts",
                newName: "ImportReceipt");

            migrationBuilder.RenameTable(
                name: "ImportDetails",
                newName: "ImportDetail");

            migrationBuilder.RenameTable(
                name: "Foods",
                newName: "Food");

            migrationBuilder.RenameTable(
                name: "FoodCategories",
                newName: "FoodCategory");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "Customer");

            migrationBuilder.RenameTable(
                name: "Bills",
                newName: "Bill");

            migrationBuilder.RenameTable(
                name: "BillInfos",
                newName: "BillInfo");

            migrationBuilder.RenameTable(
                name: "Accounts",
                newName: "Account");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermissions_IdPermission",
                table: "RolePermission",
                newName: "IX_RolePermission_IdPermission");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_IdIngredient",
                table: "Recipe",
                newName: "IX_Recipe_IdIngredient");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_IdFood",
                table: "Recipe",
                newName: "IX_Recipe_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrders_IdBill",
                table: "KitchenOrder",
                newName: "IX_KitchenOrder_IdBill");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrderDetails_IdKitchenOrder",
                table: "KitchenOrderDetail",
                newName: "IX_KitchenOrderDetail_IdKitchenOrder");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrderDetails_IdFood",
                table: "KitchenOrderDetail",
                newName: "IX_KitchenOrderDetail_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_ImportReceipts_UserName",
                table: "ImportReceipt",
                newName: "IX_ImportReceipt_UserName");

            migrationBuilder.RenameIndex(
                name: "IX_ImportReceipts_IdSupplier",
                table: "ImportReceipt",
                newName: "IX_ImportReceipt_IdSupplier");

            migrationBuilder.RenameIndex(
                name: "IX_ImportDetails_IdIngredient",
                table: "ImportDetail",
                newName: "IX_ImportDetail_IdIngredient");

            migrationBuilder.RenameIndex(
                name: "IX_ImportDetails_IdImportReceipt",
                table: "ImportDetail",
                newName: "IX_ImportDetail_IdImportReceipt");

            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "Food",
                newName: "costPrice");

            migrationBuilder.RenameIndex(
                name: "IX_Foods_IdCategory",
                table: "Food",
                newName: "IX_Food_IdCategory");

            migrationBuilder.RenameIndex(
                name: "IX_Bills_IdTable",
                table: "Bill",
                newName: "IX_Bill_IdTable");

            migrationBuilder.RenameIndex(
                name: "IX_Bills_IdCustomer",
                table: "Bill",
                newName: "IX_Bill_IdCustomer");

            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "BillInfo",
                newName: "costPrice");

            migrationBuilder.RenameIndex(
                name: "IX_BillInfos_IdFood",
                table: "BillInfo",
                newName: "IX_BillInfo_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_BillInfos_IdBill",
                table: "BillInfo",
                newName: "IX_BillInfo_IdBill");

            migrationBuilder.RenameIndex(
                name: "IX_Accounts_IdRole",
                table: "Account",
                newName: "IX_Account_IdRole");

            migrationBuilder.AlterColumn<double>(
                name: "costPrice",
                table: "Food",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<double>(
                name: "costPrice",
                table: "BillInfo",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Account",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TableFood",
                table: "TableFood",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                table: "Role",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermission",
                table: "RolePermission",
                columns: new[] { "IdRole", "IdPermission" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recipe",
                table: "Recipe",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permission",
                table: "Permission",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenOrder",
                table: "KitchenOrder",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenOrderDetail",
                table: "KitchenOrderDetail",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ingredient",
                table: "Ingredient",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceipt",
                table: "ImportReceipt",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportDetail",
                table: "ImportDetail",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Food",
                table: "Food",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoodCategory",
                table: "FoodCategory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customer",
                table: "Customer",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bill",
                table: "Bill",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillInfo",
                table: "BillInfo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Account",
                table: "Account",
                column: "UserName");

            migrationBuilder.AddForeignKey(
                name: "FK_Account_Role_IdRole",
                table: "Account",
                column: "IdRole",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bill_Customer_IdCustomer",
                table: "Bill",
                column: "IdCustomer",
                principalTable: "Customer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill",
                column: "IdTable",
                principalTable: "TableFood",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillInfo_Bill_IdBill",
                table: "BillInfo",
                column: "IdBill",
                principalTable: "Bill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillInfo_Food_IdFood",
                table: "BillInfo",
                column: "IdFood",
                principalTable: "Food",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Food_FoodCategory_IdCategory",
                table: "Food",
                column: "IdCategory",
                principalTable: "FoodCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetail_ImportReceipt_IdImportReceipt",
                table: "ImportDetail",
                column: "IdImportReceipt",
                principalTable: "ImportReceipt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetail_Ingredient_IdIngredient",
                table: "ImportDetail",
                column: "IdIngredient",
                principalTable: "Ingredient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipt_Account_UserName",
                table: "ImportReceipt",
                column: "UserName",
                principalTable: "Account",
                principalColumn: "UserName",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipt_Supplier_IdSupplier",
                table: "ImportReceipt",
                column: "IdSupplier",
                principalTable: "Supplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrder_Bill_IdBill",
                table: "KitchenOrder",
                column: "IdBill",
                principalTable: "Bill",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderDetail_Food_IdFood",
                table: "KitchenOrderDetail",
                column: "IdFood",
                principalTable: "Food",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderDetail_KitchenOrder_IdKitchenOrder",
                table: "KitchenOrderDetail",
                column: "IdKitchenOrder",
                principalTable: "KitchenOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipe_Food_IdFood",
                table: "Recipe",
                column: "IdFood",
                principalTable: "Food",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipe_Ingredient_IdIngredient",
                table: "Recipe",
                column: "IdIngredient",
                principalTable: "Ingredient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermission_Permission_IdPermission",
                table: "RolePermission",
                column: "IdPermission",
                principalTable: "Permission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermission_Role_IdRole",
                table: "RolePermission",
                column: "IdRole",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_Role_IdRole",
                table: "Account");

            migrationBuilder.DropForeignKey(
                name: "FK_Bill_Customer_IdCustomer",
                table: "Bill");

            migrationBuilder.DropForeignKey(
                name: "FK_Bill_TableFood_IdTable",
                table: "Bill");

            migrationBuilder.DropForeignKey(
                name: "FK_BillInfo_Bill_IdBill",
                table: "BillInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_BillInfo_Food_IdFood",
                table: "BillInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_Food_FoodCategory_IdCategory",
                table: "Food");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetail_ImportReceipt_IdImportReceipt",
                table: "ImportDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportDetail_Ingredient_IdIngredient",
                table: "ImportDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipt_Account_UserName",
                table: "ImportReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_ImportReceipt_Supplier_IdSupplier",
                table: "ImportReceipt");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrder_Bill_IdBill",
                table: "KitchenOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderDetail_Food_IdFood",
                table: "KitchenOrderDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_KitchenOrderDetail_KitchenOrder_IdKitchenOrder",
                table: "KitchenOrderDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipe_Food_IdFood",
                table: "Recipe");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipe_Ingredient_IdIngredient",
                table: "Recipe");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermission_Permission_IdPermission",
                table: "RolePermission");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermission_Role_IdRole",
                table: "RolePermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TableFood",
                table: "TableFood");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Supplier",
                table: "Supplier");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermission",
                table: "RolePermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                table: "Role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recipe",
                table: "Recipe");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permission",
                table: "Permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenOrderDetail",
                table: "KitchenOrderDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KitchenOrder",
                table: "KitchenOrder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ingredient",
                table: "Ingredient");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportReceipt",
                table: "ImportReceipt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImportDetail",
                table: "ImportDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FoodCategory",
                table: "FoodCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Food",
                table: "Food");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customer",
                table: "Customer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BillInfo",
                table: "BillInfo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bill",
                table: "Bill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Account",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Account");

            migrationBuilder.RenameTable(
                name: "TableFood",
                newName: "TableFoods");

            migrationBuilder.RenameTable(
                name: "Supplier",
                newName: "Suppliers");

            migrationBuilder.RenameTable(
                name: "RolePermission",
                newName: "RolePermissions");

            migrationBuilder.RenameTable(
                name: "Role",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "Recipe",
                newName: "Recipes");

            migrationBuilder.RenameTable(
                name: "Permission",
                newName: "Permissions");

            migrationBuilder.RenameTable(
                name: "KitchenOrderDetail",
                newName: "KitchenOrderDetails");

            migrationBuilder.RenameTable(
                name: "KitchenOrder",
                newName: "KitchenOrders");

            migrationBuilder.RenameTable(
                name: "Ingredient",
                newName: "Ingredients");

            migrationBuilder.RenameTable(
                name: "ImportReceipt",
                newName: "ImportReceipts");

            migrationBuilder.RenameTable(
                name: "ImportDetail",
                newName: "ImportDetails");

            migrationBuilder.RenameTable(
                name: "FoodCategory",
                newName: "FoodCategories");

            migrationBuilder.RenameTable(
                name: "Food",
                newName: "Foods");

            migrationBuilder.RenameTable(
                name: "Customer",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "BillInfo",
                newName: "BillInfos");

            migrationBuilder.RenameTable(
                name: "Bill",
                newName: "Bills");

            migrationBuilder.RenameTable(
                name: "Account",
                newName: "Accounts");

            migrationBuilder.RenameIndex(
                name: "IX_RolePermission_IdPermission",
                table: "RolePermissions",
                newName: "IX_RolePermissions_IdPermission");

            migrationBuilder.RenameIndex(
                name: "IX_Recipe_IdIngredient",
                table: "Recipes",
                newName: "IX_Recipes_IdIngredient");

            migrationBuilder.RenameIndex(
                name: "IX_Recipe_IdFood",
                table: "Recipes",
                newName: "IX_Recipes_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrderDetail_IdKitchenOrder",
                table: "KitchenOrderDetails",
                newName: "IX_KitchenOrderDetails_IdKitchenOrder");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrderDetail_IdFood",
                table: "KitchenOrderDetails",
                newName: "IX_KitchenOrderDetails_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_KitchenOrder_IdBill",
                table: "KitchenOrders",
                newName: "IX_KitchenOrders_IdBill");

            migrationBuilder.RenameIndex(
                name: "IX_ImportReceipt_UserName",
                table: "ImportReceipts",
                newName: "IX_ImportReceipts_UserName");

            migrationBuilder.RenameIndex(
                name: "IX_ImportReceipt_IdSupplier",
                table: "ImportReceipts",
                newName: "IX_ImportReceipts_IdSupplier");

            migrationBuilder.RenameIndex(
                name: "IX_ImportDetail_IdIngredient",
                table: "ImportDetails",
                newName: "IX_ImportDetails_IdIngredient");

            migrationBuilder.RenameIndex(
                name: "IX_ImportDetail_IdImportReceipt",
                table: "ImportDetails",
                newName: "IX_ImportDetails_IdImportReceipt");

            migrationBuilder.RenameColumn(
                name: "costPrice",
                table: "Foods",
                newName: "CostPrice");

            migrationBuilder.RenameIndex(
                name: "IX_Food_IdCategory",
                table: "Foods",
                newName: "IX_Foods_IdCategory");

            migrationBuilder.RenameColumn(
                name: "costPrice",
                table: "BillInfos",
                newName: "CostPrice");

            migrationBuilder.RenameIndex(
                name: "IX_BillInfo_IdFood",
                table: "BillInfos",
                newName: "IX_BillInfos_IdFood");

            migrationBuilder.RenameIndex(
                name: "IX_BillInfo_IdBill",
                table: "BillInfos",
                newName: "IX_BillInfos_IdBill");

            migrationBuilder.RenameIndex(
                name: "IX_Bill_IdTable",
                table: "Bills",
                newName: "IX_Bills_IdTable");

            migrationBuilder.RenameIndex(
                name: "IX_Bill_IdCustomer",
                table: "Bills",
                newName: "IX_Bills_IdCustomer");

            migrationBuilder.RenameIndex(
                name: "IX_Account_IdRole",
                table: "Accounts",
                newName: "IX_Accounts_IdRole");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "Foods",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPrice",
                table: "BillInfos",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TableFoods",
                table: "TableFoods",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "IdRole", "IdPermission" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recipes",
                table: "Recipes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenOrderDetails",
                table: "KitchenOrderDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KitchenOrders",
                table: "KitchenOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ingredients",
                table: "Ingredients",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportReceipts",
                table: "ImportReceipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImportDetails",
                table: "ImportDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FoodCategories",
                table: "FoodCategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Foods",
                table: "Foods",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BillInfos",
                table: "BillInfos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bills",
                table: "Bills",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Accounts",
                table: "Accounts",
                column: "UserName");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Roles_IdRole",
                table: "Accounts",
                column: "IdRole",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillInfos_Bills_IdBill",
                table: "BillInfos",
                column: "IdBill",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BillInfos_Foods_IdFood",
                table: "BillInfos",
                column: "IdFood",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Customers_IdCustomer",
                table: "Bills",
                column: "IdCustomer",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_TableFoods_IdTable",
                table: "Bills",
                column: "IdTable",
                principalTable: "TableFoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Foods_FoodCategories_IdCategory",
                table: "Foods",
                column: "IdCategory",
                principalTable: "FoodCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetails_ImportReceipts_IdImportReceipt",
                table: "ImportDetails",
                column: "IdImportReceipt",
                principalTable: "ImportReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportDetails_Ingredients_IdIngredient",
                table: "ImportDetails",
                column: "IdIngredient",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Accounts_UserName",
                table: "ImportReceipts",
                column: "UserName",
                principalTable: "Accounts",
                principalColumn: "UserName",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportReceipts_Suppliers_IdSupplier",
                table: "ImportReceipts",
                column: "IdSupplier",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderDetails_Foods_IdFood",
                table: "KitchenOrderDetails",
                column: "IdFood",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrderDetails_KitchenOrders_IdKitchenOrder",
                table: "KitchenOrderDetails",
                column: "IdKitchenOrder",
                principalTable: "KitchenOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KitchenOrders_Bills_IdBill",
                table: "KitchenOrders",
                column: "IdBill",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Foods_IdFood",
                table: "Recipes",
                column: "IdFood",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Ingredients_IdIngredient",
                table: "Recipes",
                column: "IdIngredient",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_IdPermission",
                table: "RolePermissions",
                column: "IdPermission",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_IdRole",
                table: "RolePermissions",
                column: "IdRole",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
