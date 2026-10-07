using CafeManagement.API.Controllers;
using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
int count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE") ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;") { InitialCatalog = "CafeShiftChecks_" + Guid.NewGuid().ToString("N") };
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options;
await using var db = new AppDbContext(options);
async Task Reject(Func<Task> task, string name) { try { await task(); } catch (InvalidOperationException) { db.ChangeTracker.Clear(); Check(true, name); return; } throw new Exception(name); }
ControllerContext Identity(string name, params string[] codes) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, name) }.Concat(codes.Select(c => new Claim("Permission", c))), "Test")) } };
try {
    await db.Database.MigrateAsync(); Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches shifts model");
    var role = new Role { Name = "Shift fixture" };
    db.Accounts.AddRange(new Account { UserName = "cashier", DisplayName = "Cashier", PassWord = "fixture", Role = role }, new Account { UserName = "other", PassWord = "fixture", Role = role });
    var food = new Food { Name = "Coffee", Price = 50000, Category = new FoodCategory { Name = "Coffee" } };
    var bill = new Bill { OrderType = "Takeaway", BillInfos = [new() { Food = food, Count = 1, UnitPrice = 50000 }] };
    db.Bills.Add(bill); await db.SaveChangesAsync();
    var checkout = new BillController(db) { ControllerContext = Identity("cashier", "POS_VIEW", "POS_CHECKOUT") };
    Check(await checkout.Checkout(bill.Id, new() { PaymentMethod = "Cash" }) is BadRequestObjectResult, "Authenticated checkout requires active shift"); db.ChangeTracker.Clear();
    Check(!await db.CashEntries.AnyAsync() && !await db.KitchenOrders.AnyAsync() && (await db.Bills.FindAsync(bill.Id))!.Status == 0, "Missing shift leaves stock/kitchen/cash/payment unchanged"); db.ChangeTracker.Clear();
    var flow = new ShiftFlow(db); var key = Guid.NewGuid().ToString();
    var first = await flow.Open("cashier", 200000, "Start", key); db.ChangeTracker.Clear();
    Check(await flow.Open("cashier", 200000, "Start", key) == first && await db.CashierShifts.CountAsync() == 1, "Open retry returns same shift without duplicate");
    Check(await db.CashEntries.CountAsync() == 0, "Opening float creates no duplicate cashbook receipt");
    await Reject(() => flow.Open("cashier", 0, "", Guid.NewGuid().ToString()), "Second active shift rejected");
    await Reject(() => flow.Open("other", -1, "", Guid.NewGuid().ToString()), "Negative opening cash rejected");
    Check(await checkout.Checkout(bill.Id, new() { PaymentMethod = "Cash" }) is OkObjectResult, "Checkout succeeds inside active shift"); db.ChangeTracker.Clear();
    Check((await db.Bills.FindAsync(bill.Id))!.IdShift == first && (await db.CashEntries.SingleAsync()).IdShift == first, "Paid bill and cash receipt share cashier shift"); db.ChangeTracker.Clear();
    var ledger = new CashbookController(db) { ControllerContext = Identity("cashier") };
    Check(await ledger.Create(new() { Direction = "Out", Amount = 20000, Category = "Other", Note = "Expense", PaymentMethod = "Cash", RequestKey = Guid.NewGuid().ToString() }) is OkObjectResult, "Manual cash expense attaches to active shift");
    Check(await ledger.Create(new() { Direction = "In", Amount = 100000, Category = "Other", Note = "Transfer", PaymentMethod = "Transfer", RequestKey = Guid.NewGuid().ToString() }) is OkObjectResult, "Transfer income attaches to shift separately"); db.ChangeTracker.Clear();
    var supplier = new Supplier { Name = "Shift supplier" }; db.Suppliers.Add(supplier); await db.SaveChangesAsync();
    var receipt = new ImportReceipt { IdSupplier = supplier.Id, UserName = "cashier", TotalAmount = 100000 }; db.ImportReceipts.Add(receipt); await db.SaveChangesAsync();
    var paymentId = await new WarehouseFlow(db).Pay(receipt.Id, new() { Amount = 5000, PaymentMethod = "Transfer", RequestKey = Guid.NewGuid().ToString() }, "cashier"); db.ChangeTracker.Clear();
    Check((await db.CashEntries.FindAsync(paymentId))!.IdShift == first, "Supplier debt payment attaches to cashier shift"); db.ChangeTracker.Clear();
    var totals = await flow.Totals((await db.CashierShifts.FindAsync(first))!);
    Check(totals.ExpectedCash == 230000 && totals.Bills == 1 && totals.Sales == 50000 && totals.Methods.Single(m => m.PaymentMethod == "Transfer").TotalIn == 100000, "Expected cash excludes transfer and includes cash income/expense");
    await Reject(() => flow.Close(first, "cashier", false, 230000, "", 200000), "Stale reconciliation rejected before closing");
    await Reject(() => flow.Close(first, "cashier", false, 229000, "", 230000), "Variance requires explanation");
    var otherController = new ShiftController(db) { ControllerContext = Identity("other", "SHIFT_SELF") };
    Check(await otherController.Detail(first) is ForbidResult && await otherController.Close(first, new() { CountedCash = 230000, ExpectedCash = 230000 }) is ForbidResult, "Employee cannot read/close another cashier shift");
    await flow.Close(first, "cashier", false, 229000, "Short 1000", 230000); db.ChangeTracker.Clear();
    var closed = (await db.CashierShifts.FindAsync(first))!;
    Check(closed.Difference == -1000 && closed.ClosedBy == "cashier" && closed.ClosedAt != null, "Close stores counted amount variance explanation and actor");
    await flow.Close(first, "cashier", false, 229000, "Short 1000", 230000);
    Check(await db.CashierShifts.CountAsync() == 1, "Close retry leaves immutable snapshot intact");
    await Reject(() => flow.Close(first, "cashier", false, 230000, "Edited", 230000), "Closed shift cannot be edited");
    var second = await flow.Open("cashier", 229000, "Next", Guid.NewGuid().ToString()); db.ChangeTracker.Clear();
    await new InvoiceFlow(db).Refund(bill.Id, new() { Reason = "Next shift refund", PaymentMethod = "Cash" }, "cashier"); db.ChangeTracker.Clear();
    Check((await db.CashEntries.SingleAsync(c => c.Direction == "Out" && c.IdBill == bill.Id)).IdShift == second && (await db.Bills.FindAsync(bill.Id))!.IdShift == first, "Later refund belongs to new shift while original sale stays in old shift"); db.ChangeTracker.Clear();
    Check((await flow.Totals((await db.CashierShifts.FindAsync(first))!)).ExpectedCash == 230000 && (await flow.Totals((await db.CashierShifts.FindAsync(first))!)).Sales == 50000, "Closed totals remain frozen after later refund");
    var next = await flow.Totals((await db.CashierShifts.FindAsync(second))!);
    Check(next.Refunds == 50000 && next.ExpectedCash == 179000 && next.Bills == 0, "New shift records refund without inventing sale");
    var ownerController = new ShiftController(db) { ControllerContext = Identity("cashier", "SHIFT_SELF") };
    var details = JsonSerializer.SerializeToElement(((OkObjectResult)await ownerController.Detail(first)).Value);
    Check(details.GetProperty("Totals").GetProperty("ExpectedCash").GetDecimal() == 230000, "History API reads saved close snapshot");
    Check(await ownerController.List(status: "wrong") is BadRequestObjectResult, "History validates filters");
    // Two independent processes/contexts racing to open one account: exactly one wins.
    async Task<bool> Race() { await using var ctx = new AppDbContext(options); try { await new ShiftFlow(ctx).Open("other", 0, "", Guid.NewGuid().ToString()); return true; } catch (InvalidOperationException) { return false; } }
    var races = await Task.WhenAll(Race(), Race()); Check(races.Count(x => x) == 1, "Concurrent open permits exactly one active shift per account");
    // Closing and posting a transaction serialize; no transaction can attach to a closed shift.
    async Task Post() { await using var ctx = new AppDbContext(options); var c = new CashbookController(ctx) { ControllerContext = Identity("cashier") }; await c.Create(new() { Direction = "In", Amount = 10000, PaymentMethod = "Cash", Category = "Other", Note = "Race", RequestKey = Guid.NewGuid().ToString() }); }
    async Task<bool> CloseRace() { await using var ctx = new AppDbContext(options); try { await new ShiftFlow(ctx).Close(second, "cashier", false, 179000, "", 179000); return true; } catch (InvalidOperationException) { return false; } }
    var closeTask = CloseRace(); await Post(); var raceClosed = await closeTask; db.ChangeTracker.Clear();
    var raceShift = (await db.CashierShifts.FindAsync(second))!;
    Check(raceClosed ? (await flow.Totals(raceShift)).ExpectedCash == 179000 && !await db.CashEntries.AnyAsync(c => c.IdShift == second && c.Note == "Race") : raceShift.ClosedAt == null && (await flow.Totals(raceShift)).ExpectedCash == 189000, "Concurrent cash post either precedes close and invalidates stale total or remains outside closed shift");
    Check(await db.CashEntries.CountAsync(c => c.IdShift == first) == 4, "No later financial write appended to first closed shift");
    Console.WriteLine($"All {count} shift checks passed.");
} finally { await db.Database.EnsureDeletedAsync(); }
