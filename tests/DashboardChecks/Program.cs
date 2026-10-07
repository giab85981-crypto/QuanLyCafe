using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using CafeManagement.API.Data;
using CafeManagement.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
var today = new DateTime(2026, 10, 3);
var coffee = new Food { Id = 1, Name = "Coffee", Price = 99000, IdCategory = 1,
    Category = new FoodCategory { Id = 1, Name = "Drinks" }, ItemType = "Prepared", MenuKind = "Đồ uống" };
var cake = new Food { Id = 2, Name = "Cake", Price = 50000, IdCategory = 2,
    Category = new FoodCategory { Id = 2, Name = "Cakes" }, ItemType = "Goods", MenuKind = "Đồ ăn" };
var paid = new Bill { Status = 1, DateCheckIn = today.AddDays(-8), DateCheckOut = today.AddHours(10),
    Discount = 10, TotalPrice = 90000, GuestCount = 3,
    BillInfos = new List<BillInfo> { new() { IdFood = 1, Food = coffee, Count = 2, SentCount = 2, UnitPrice = 25000 }, new() { IdFood = 2, Food = cake, Count = 1, UnitPrice = 50000 } } };
var free = new Bill { Status = 1, DateCheckIn = today, DateCheckOut = today.AddHours(11), Discount = 100,
    TotalPrice = 0, GuestCount = 2, BillInfos = new List<BillInfo> { new() { IdFood = 1, Food = coffee, Count = 1, UnitPrice = 25000 } } };
var old = new Bill { Status = 1, DateCheckIn = today.AddDays(-20), TotalPrice = 60000,
    BillInfos = new List<BillInfo> { new() { IdFood = 2, Food = cake, Count = 10, UnitPrice = 50000 } } };
var unpaid = new Bill { Status = 0, DateCheckIn = today, TotalPrice = 500000, GuestCount = 30 };
var future = new Bill { Status = 1, DateCheckIn = today.AddDays(1), TotalPrice = 500000, GuestCount = 30 };
var bills = new[] { paid, free, old, unpaid, future };
Check(DashboardCalculator.NetRevenue(paid) == 90000, "Recorded revenue and historical unit price survive current price changes");
var chart = DashboardCalculator.Chart(bills, today, 7, "day");
Check(chart.Revenue == 90000 && chart.OrderCount == 2 && chart.GuestCount == 5 && chart.Points.Count == 7, "Date range uses checkout and excludes unpaid/future bills");
Check(chart.Points.Sum(p => p.Revenue) == chart.Revenue && chart.Points.Sum(p => p.GuestCount) == 5, "Daily totals reconcile");
var hours = DashboardCalculator.Chart(bills, today, 7, "hour");
Check(hours.Points.Count == 24 && hours.Points[10].Revenue == 90000 && hours.Points[11].GuestCount == 2, "Hourly buckets");
var weekdays = DashboardCalculator.Chart(bills, today, 7, "weekday");
Check(weekdays.Points.Count == 7 && weekdays.Points[5].Revenue == 90000, "Weekday buckets start Monday");
var menu = DashboardCalculator.Menu(bills, today, 7, "category", null, "quantity");
Check(menu.Sales.Sum(s => s.TotalRevenue) == 90000 && menu.TopSellingFoods[0].FoodId == 1
    && menu.TopSellingFoods[0].QuantitySold == 3, "Menu discounts reconcile, old sales excluded, free items counted");
Check(menu.AverageFoodValue == 45000 && menu.AverageDrinkValue == 15000 && menu.AverageItemValue == 22500, "Weighted averages use sold quantities");
Check(DashboardCalculator.Menu(bills, today, 7, "type", "Goods", "revenue").TopSellingFoods.Single().FoodId == 2, "Type filter and revenue sorting");
Check(DashboardCalculator.Menu(bills, today, 7, "category", "missing", "quantity").TopSellingFoods.Count == 0, "Empty group filter");
Check(DashboardCalculator.Chart(Array.Empty<Bill>(), today, 1, "day").Revenue == 0
    && DashboardCalculator.Menu(Array.Empty<Bill>(), today, 7, "category", null, "quantity").AverageItemValue == 0, "Empty data and zero denominators");
var legacy = new Bill { Status = 1, DateCheckIn = today, GuestCount = null, Discount = 20,
    BillInfos = new List<BillInfo> { new() { IdFood = 2, Food = cake, Count = 1 } } };
var legacyChart = DashboardCalculator.Chart(new[] { legacy }, today, 7, "day");
Check(legacyChart.Revenue == 40000 && legacyChart.EstimatedRevenueOrders == 1 && legacyChart.UnknownGuestOrders == 1, "Legacy revenue is marked estimated and guests remain unknown");
Check(DashboardCalculator.NetRevenue(free) == 0 && !DashboardCalculator.IsEstimated(free), "100 percent discounts stay zero");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=QuanLyCafe_V2;Trusted_Connection=True;TrustServerCertificate=True;").Options;
using var context = new AppDbContext(options);
var api = new DashboardController(context);
Check(await api.GetOverview(days: 0) is BadRequestObjectResult && await api.GetOverview(days: 367) is BadRequestObjectResult
    && await api.GetOverview(revenueGroup: "invalid") is BadRequestObjectResult, "API validates periods and grouping before querying");
var assembly = context.GetService<IMigrationsAssembly>();
var snapshot = assembly.ModelSnapshot!.Model;
var initializer = context.GetService<IModelRuntimeInitializer>();
var initialized = initializer.Initialize(snapshot, designTime: true);
var model = context.GetService<IDesignTimeModel>().Model;
var differences = context.GetService<IMigrationsModelDiffer>().GetDifferences(initialized.GetRelationalModel(), model.GetRelationalModel());
Check(differences.Count == 0, "Migration snapshot matches runtime model");
var script = context.GetService<IMigrator>().GenerateScript("20261002031845_AddItemTypeToFood", "20261003090000_DashboardSalesTracking", MigrationsSqlGenerationOptions.Idempotent);
if (args.Length > 0) File.WriteAllText(args[0], script);
Check(script.Contains("GuestCount") && script.Contains("UnitPrice") && script.Contains("MenuKind") && script.Contains("ItemType"), "Migration SQL includes required schema changes");
Console.WriteLine("All Dashboard checks passed.");
if (args.Contains("--database"))
{
    Check(context.Database.GetPendingMigrations().Count() == 0, "LocalDB has current migration");
    await using var transaction = await context.Database.BeginTransactionAsync();
    try
    {
        var before = (CafeManagement.API.Dtos.DashboardOverviewResponse)((OkObjectResult)(await api.GetOverview())).Value!;
        var testTable = new TableFood { Name = "Dashboard verification", IsActive = true, Status = "Có người" };
        var testFood = new Food { Name = "Dashboard verification", Price = 99000, MenuKind = "Đồ uống",
            Category = new FoodCategory { Name = "Dashboard verification" }, ItemType = "Món chế biến" };
        var testBill = new Bill { TableFood = testTable, DateCheckIn = DateTime.Now, Status = 0,
            BillInfos = new List<BillInfo> { new() { Food = testFood, Count = 2, SentCount = 2, UnitPrice = 21000 } } };
        context.Bills.Add(testBill);
        await context.SaveChangesAsync();
        var checkout = new BillController(context);
        Check(await checkout.UpdateGuests(testBill.Id, new() { GuestCount = 4 }) is OkObjectResult, "Persist serving guest count");
        Check(await checkout.Checkout(testBill.Id, new() { Discount = 10, GuestCount = 4 }) is OkObjectResult,
            "Checkout succeeds against LocalDB");
        Check(testBill.TotalPrice == 37800 && testBill.GuestCount == 4 && testTable.Status == "Trống",
            "Checkout persists discounted historical price, guests and table status");
        var after = (CafeManagement.API.Dtos.DashboardOverviewResponse)((OkObjectResult)(await api.GetOverview())).Value!;
        Check(after.Summary.TodayRevenue - before.Summary.TodayRevenue == 37800
            && after.Summary.TodayOrderCount - before.Summary.TodayOrderCount == 1
            && after.Customers.GuestCount - before.Customers.GuestCount == 4,
            "Dashboard API and checkout reconcile on real LocalDB");
        var types = new ItemTypeController(context);
        Check(await types.GetAll() is OkObjectResult, "ItemType catalog reads successfully");
    }
    finally { await transaction.RollbackAsync(); }
    Console.WriteLine("LocalDB integration checks passed; test data rolled back.");
}
Check(DevelopmentLocalDb.FindPipe("State: Running\nInstance pipe name: np:\\\\.\\pipe\\LOCALDB#123ABC\\tsql\\query\r\n")
    == @"np:\\.\pipe\LOCALDB#123ABC\tsql\query", "LocalDB pipe parser handles CLI output");
Check(DevelopmentLocalDb.FindPipe("State: Stopped\nInstance pipe name:") == null
    && DevelopmentLocalDb.FindPipe(@"np:\\.\pipe\LOCALDB#invalid\other") == null,
    "LocalDB pipe parser rejects stopped and malformed outputs");
var external = "Server=localhost;Database=sample;Integrated Security=true";
Check(await DevelopmentLocalDb.ResolveAsync(external) == external, "LocalDB resolver leaves other SQL servers unchanged");
if (args.Contains("--localdb"))
{
    var resolved = await DevelopmentLocalDb.ResolveAsync("Server=(localdb)\\MSSQLLocalDB;Database=QuanLyCafe_V2;Integrated Security=true;TrustServerCertificate=true;");
    await using var sql = new Microsoft.Data.SqlClient.SqlConnection(resolved);
    await sql.OpenAsync();
    await using var command = sql.CreateCommand();
    command.CommandText = "SELECT DB_NAME()";
    Check((string)(await command.ExecuteScalarAsync())! == "QuanLyCafe_V2", "Resolved pipe connects to original database");
}
Check(DevelopmentLocalDb.FindPipe(@"Instance pipe name: \\.\pipe\LOCALDB#ABCDEF\tsql\query") == @"np:\\.\pipe\LOCALDB#ABCDEF\tsql\query", "LocalDB parser accepts output without np prefix");
var unicodePipe = string.Join("\0", @"np:\\.\pipe\LOCALDB#ABCDEF\tsql\query".ToCharArray());
Check(DevelopmentLocalDb.FindPipe(unicodePipe) == @"np:\\.\pipe\LOCALDB#ABCDEF\tsql\query", "LocalDB parser accepts NUL-interleaved CLI output");


Check(DevelopmentLocalDb.FindPipeInLog(@"Server local connection provider is ready to accept connection on [ \\.\pipe\LOCALDB#B9803E73\tsql\query ].") == @"np:\\.\pipe\LOCALDB#B9803E73\tsql\query", "LocalDB log parser accepts brackets and trailing text");
Check(DevelopmentLocalDb.FindPipeInLog(@"\\.\pipe\LOCALDB#invalid/other\tsql\query") == null && DevelopmentLocalDb.FindPipeInLog("State: Stopped") == null, "LocalDB log parser rejects unrelated and malformed pipes");
