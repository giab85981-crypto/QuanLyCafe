using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using CafeManagement.API.Controllers;
using CafeManagement.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json;

int count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
var day = new DateTime(2026, 10, 6);
var food = new Food { Name = "Current name", Price = 999999 };
var bill = new Bill { Status = 3, HasRecordedPayment = true, DateCheckOut = day.AddDays(-1).AddHours(23), CancelledAt = day.AddHours(10), TotalPrice = 90000, RefundAmount = 90000,
    BillInfos = [new() { IdFood = 1, Food = food, FoodNameSnapshot = "Historical coffee", Count = 2, UnitPrice = 50000, CostPrice = 10000 }] };
var paid = new Bill { Status = 1, HasRecordedPayment = true, DateCheckOut = day.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999), TotalPrice = 40000,
    BillInfos = [new() { IdFood = 2, Food = food, Count = 1, UnitPrice = 50000, CostPrice = 15000 }] };
var open = new Bill { Status = 0, DateCheckOut = day, TotalPrice = 1000000 };
var report = FinancialReports.Sales([bill, paid, open], day, day.AddDays(1));
Check(report.Sales == 40000 && report.Refunds == 90000 && report.NetRevenue == -50000, "Cross-day refund belongs to refund date; inclusive last millisecond; open orders excluded");
Check(report.Cost == 15000 && report.GrossProfit == -65000, "Refund does not restore consumed cost");
var prior = FinancialReports.Sales([bill], day.AddDays(-1), day);
Check(prior.Sales == 90000 && prior.Refunds == 0 && prior.Cost == 20000, "Refund does not rewrite original sale day");
Check(prior.Foods.Single().TotalAmount == 90000 && prior.Foods.Single().FoodName == "Historical coffee", "Food totals allocate discounts and use snapshot names/prices");
var chart = DashboardCalculator.Chart([bill, paid], day, 1, "hour");
Check(chart.Revenue == report.NetRevenue && chart.Points.Sum(p => p.Revenue) == report.NetRevenue && chart.Points[10].Revenue == -90000, "Dashboard buckets reconcile with financial report including negative refund-only hour");
var free = new Bill { Status = 1, HasRecordedPayment = true, DateCheckOut = day, Discount = 100, BillInfos = [new() { IdFood = 1, Food = food, Count = 1, UnitPrice = 10000 }] };
Check(FinancialReports.Sales([free], day, day.AddDays(1)).EstimatedBills == 0, "Recorded free order remains zero, not estimated");
Check(FinancialReports.Range(day.AddHours(5), day.AddHours(7)) == (day, day.AddDays(1)), "Date range uses full inclusive days");
foreach (var (from, to) in new[] { (day, day.AddDays(-1)), (day, day.AddDays(366)), (default(DateTime), day) }) {
    try { FinancialReports.Range(from, to); throw new Exception("Invalid range accepted"); } catch (InvalidOperationException) { count++; }
}
var cash = new[] { new CashEntry { CreatedAt = day.AddDays(-1), Direction = "In", Amount = 100000 }, new CashEntry { CreatedAt = day, Direction = "Out", Amount = 20000 }, new CashEntry { CreatedAt = day, Direction = "In", Amount = 40000, PaymentMethod = "Transfer" }, new CashEntry { CreatedAt = day.AddDays(1), Direction = "In", Amount = 999999 } };
var cashJson = JsonSerializer.SerializeToElement(FinancialReports.Cash(cash, day, day.AddDays(1)));
var cashMethod = cashJson.EnumerateArray().Single(c => c.GetProperty("PaymentMethod").GetString() == "Cash");
Check(cashMethod.GetProperty("Opening").GetDecimal() == 100000 && cashMethod.GetProperty("Closing").GetDecimal() == 80000, "Cash opening and closing exclude future entries and keep methods separate");
if (args.Contains("--database")) {
    var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE") ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;") { InitialCatalog = "CafeFinancialChecks_" + Guid.NewGuid().ToString("N") };
    await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
    try {
        await db.Database.MigrateAsync();
        var category = new FoodCategory { Name = "Financial test" }; food.Category = category;
        db.AddRange(bill, paid, new Ingredient { Name = "Milk", Unit = "ml", Quantity = 5, MinQuantity = 10, Lots = [new() { Quantity = 5, UnitCost = 100, ExpiryDate = DateTime.Today.AddDays(-1) }] });
        // Both bills reference the same newly generated food identity.
        foreach (var line in bill.BillInfos.Concat(paid.BillInfos)) line.IdFood = 0;
        db.CashEntries.AddRange(cash);
        db.Bills.Add(new Bill { Status = 1, DateCheckOut = day, Discount = 0, BillInfos = [new() { Food = food, Count = 1, UnitPrice = 10000 }] });
        var role = new Role { Name = "Finance fixture" };
        var account = new Account { UserName = "finance.fixture", PassWord = "fixture", Role = role };
        var supplier = new Supplier { Name = "Supplier fixture" };
        var receipt = new ImportReceipt { Supplier = supplier, Account = account, TotalAmount = 100000, HasPaymentTracking = true };
        db.ImportReceipts.AddRange(receipt, new ImportReceipt { Supplier = supplier, Account = account, TotalAmount = 999999, HasPaymentTracking = false });
        db.CashEntries.Add(new CashEntry { ImportReceipt = receipt, Amount = 30000, Direction = "Out", CreatedAt = day });
        await db.SaveChangesAsync();
        var controller = new ReportController(db);
        var result = (OkObjectResult)await controller.Summary(day, day);
        var json = JsonSerializer.SerializeToElement(result.Value);
        Check(json.GetProperty("Sales").GetProperty("EstimatedBills").GetInt32() == 1, "SQL report exposes legacy estimates");
        Check(json.GetProperty("Stock")[0].GetProperty("Expired").GetDouble() == 5, "SQL report identifies expired lot");
        Check(json.GetProperty("Suppliers")[0].GetProperty("Debt").GetDecimal() == 70000 && json.GetProperty("Suppliers")[0].GetProperty("Paid").GetDecimal() == 30000, "Supplier debt subtracts linked payments and excludes untracked legacy imports");
        Check(json.GetProperty("UntrackedImports").GetInt32() == 1, "Untracked legacy imports reported explicitly");
        Check(await controller.GetRevenueReport(day, day) is OkObjectResult && await controller.GetTopSelling(day, day) is OkObjectResult, "Legacy report routes execute against SQL");
        Check(await controller.Summary(day, day.AddDays(-1)) is BadRequestObjectResult && await controller.GetTopSelling(day, day, 101) is BadRequestObjectResult, "Invalid report ranges/top rejected before query");
        var ledger = (OkObjectResult)await new CashbookController(db).Get(day, day);
        var ledgerJson = JsonSerializer.SerializeToElement(ledger.Value);
        Check(ledgerJson.GetProperty("TotalIn").GetDecimal() == 40000 && ledgerJson.GetProperty("LegacyAmount").GetDecimal() == 50000, "Cashbook does not count legacy estimates or unrecorded receipts as cash");
        Check(ledgerJson.GetProperty("Methods").GetArrayLength() == 2, "Cashbook and report share method reconciliation");
    } finally { await db.Database.EnsureDeletedAsync(); }
}
Console.WriteLine($"All {count} financial checks passed.");
