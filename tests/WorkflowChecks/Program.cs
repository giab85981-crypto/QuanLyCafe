using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; Console.WriteLine("PASS " + label); }
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE")) { InitialCatalog = "CafeWorkflowChecks_" + Guid.NewGuid().ToString("N") };
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
Process? server = null;
try {
    await DbSeeder.SeedAsync(db);
    var cashierRole = await db.Roles.SingleAsync(r => r.Name == "Cashier");
    var kitchenRole = await db.Roles.SingleAsync(r => r.Name == "Kitchen");
    foreach (var name in new[] { "flow.cashier", "flow.other", "flow.kitchen" }) db.Accounts.Add(new Account { UserName = name, DisplayName = name, IdRole = name == "flow.kitchen" ? kitchenRole.Id : cashierRole.Id, PassWord = BCrypt.Net.BCrypt.HashPassword("Workflow123") });
    await db.SaveChangesAsync();
    var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var arg in new[] { Path.Combine(root, "CafeManagement.API/bin/Release/net8.0/CafeManagement.API.dll"), "--urls", $"http://localhost:{port}", "--contentRoot", Path.Combine(root, "CafeManagement.API") }) psi.ArgumentList.Add(arg);
    psi.Environment["ConnectionStrings__DefaultConnection"] = cs.ConnectionString;
    psi.Environment["LocalDb__UseNamedPipe"] = "false";
    psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    psi.Environment["Logging__LogLevel__Default"] = "Error";
    server = Process.Start(psi)!;
    var log = new StringBuilder();
    server.OutputDataReceived += (_, e) => { if (e.Data != null) lock (log) log.AppendLine(e.Data); };
    server.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.AppendLine(e.Data); };
    server.BeginOutputReadLine(); server.BeginErrorReadLine();
    var address = new Uri($"http://localhost:{port}/api/");
    using var admin = new HttpClient { BaseAddress = address };
    using var cashier = new HttpClient { BaseAddress = address };
    using var other = new HttpClient { BaseAddress = address };
    using var kitchen = new HttpClient { BaseAddress = address };
    async Task Login(HttpClient client, string name, string password) {
        var response = await client.PostAsJsonAsync("Auth/login", new { userName = name, passWord = password });
        Check(response.IsSuccessStatusCode, "Login " + name);
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>());
    }
    for (int attempt = 0; ; attempt++) {
        if (server.HasExited) throw new Exception(log.ToString());
        try { await Login(admin, "admin", "admin123"); break; }
        catch (HttpRequestException) when (attempt < 100) { await Task.Delay(100); }
    }
    await Login(cashier, "flow.cashier", "Workflow123"); await Login(other, "flow.other", "Workflow123"); await Login(kitchen, "flow.kitchen", "Workflow123");
    async Task<JsonObject> Post(HttpClient client, string path, object body) {
        using var response = await client.PostAsJsonAsync(path, body);
        if (!response.IsSuccessStatusCode) throw new Exception(path + ": " + response.StatusCode + " " + await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonObject>() ?? new JsonObject();
    }
    async Task Rights(params object[] overrides) {
        var snapshot = (await admin.GetFromJsonAsync<JsonObject>("Access"))!;
        var row = snapshot["accounts"]!.AsArray().OfType<JsonObject>().Single(a => a["userName"]!.GetValue<string>() == "flow.cashier");
        using var result = await admin.PutAsJsonAsync("Access/accounts/flow.cashier", new { idRole = cashierRole.Id, overrides, revision = row["revision"]!.GetValue<string>() });
        Check(result.IsSuccessStatusCode, "Update live cashier permissions");
    }
    var ingredient = (await Post(admin, "Warehouse/ingredients", new { name = "Workflow milk", code = "FLOW-MILK", unit = "ml", quantity = 0, unitCost = 100 }))["id"]!.GetValue<int>();
    var supplier = (await Post(admin, "Warehouse/suppliers", new { name = "Workflow supplier" }))["id"]!.GetValue<int>();
    var importKey = Guid.NewGuid().ToString();
    var importBody = new { idSupplier = supplier, paidAmount = 4000, paymentMethod = "Cash", requestKey = importKey, items = new[] { new { idIngredient = ingredient, quantity = 100, price = 100, lotCode = "FLOW-FRESH", expiryDate = DateTime.Today.AddDays(3) } } };
    var receipt = (await Post(admin, "Warehouse/receipts", importBody))["id"]!.GetValue<int>();
    Check((await Post(admin, "Warehouse/receipts", importBody))["id"]!.GetValue<int>() == receipt, "Import retry does not duplicate stock or supplier payment");
    var category = await db.FoodCategories.Select(c => c.Id).FirstAsync();
    var foodResponse = await Post(admin, "Food", new { name = "Workflow latte", code = "FLOW-LATTE", idCategory = category, price = 25000, recipe = new[] { new { idIngredient = ingredient, amount = 10 } } });
    var food = foodResponse["id"]!.GetValue<int>();
    var customer = (await Post(admin, "Customer", new { name = "Workflow customer", phone = "0901234987" }))["id"]!.GetValue<int>();
    var tables = await db.TableFoods.OrderBy(t => t.Id).Select(t => t.Id).Take(2).ToListAsync();
    var shift = (await Post(cashier, "Shift", new { openingCash = 200000, requestKey = Guid.NewGuid().ToString() }))["id"]!.GetValue<int>();
    var secondShift = (await Post(other, "Shift", new { openingCash = 50000, requestKey = Guid.NewGuid().ToString() }))["id"]!.GetValue<int>();
    var bill = (await Post(cashier, "Bill/add-item", new { idTable = tables[0], idFood = food, count = 7 }))["idBill"]!.GetValue<int>();
    Check((await other.PostAsJsonAsync("Bill/add-item", new { idTable = tables[1], idFood = food, count = 4 })).StatusCode == HttpStatusCode.BadRequest, "Another cashier cannot order beyond available reserved ingredients");
    var secondBill = (await Post(other, "Bill/add-item", new { idTable = tables[1], idFood = food, count = 3 }))["idBill"]!.GetValue<int>();
    var firstLine = await db.BillInfos.Where(i => i.IdBill == bill).Select(i => i.Id).SingleAsync();
    var secondLine = await db.BillInfos.Where(i => i.IdBill == secondBill).Select(i => i.Id).SingleAsync();
    Check((await other.PostAsJsonAsync($"Bill/items/{secondLine}/cancel", new { count = 1 })).IsSuccessStatusCode, "Unsent cancellation succeeds");
    Check((await new StockReservations(db).Held())[ingredient] == 90 && (await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 100, "Unsent cancellation releases stock promise without physical consumption");
    var ticket = (await Post(cashier, "Kitchen/send-order", new { idBill = bill }))["idKitchenOrder"]!.GetValue<int>();
    await Post(cashier, "Kitchen/send-order", new { idBill = bill });
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 30 && await db.KitchenOrders.CountAsync(o => o.IdBill == bill) == 1, "Kitchen report retry consumes ingredients once");
    Check((await cashier.PutAsJsonAsync($"Kitchen/{ticket}/status", new { status = "Cooking" })).StatusCode == HttpStatusCode.Forbidden, "Cashier cannot start kitchen work without permission");
    Check((await kitchen.PostAsJsonAsync("Bill/add-item", new { idTable = tables[0], idFood = food, count = 1 })).StatusCode == HttpStatusCode.Forbidden, "Kitchen account cannot add sale items");
    Check((await cashier.PostAsJsonAsync($"Bill/items/{firstLine}/cancel", new { count = 1, reason = "Customer changed order" })).IsSuccessStatusCode, "Cancel pending item returns ingredients");
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 40, "Pending cancellation restores ten ml to original lot");
    Check((await kitchen.PutAsJsonAsync($"Kitchen/{ticket}/status", new { status = "Cooking" })).IsSuccessStatusCode, "Kitchen starts preparation");
    Check((await cashier.PostAsJsonAsync($"Bill/items/{firstLine}/cancel", new { count = 1, reason = "Customer cancelled prepared item" })).IsSuccessStatusCode, "Cancel cooking item records waste");
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 40 && await db.StockMovements.AnyAsync(m => m.IdIngredient == ingredient && m.Kind == "Waste"), "Prepared cancellation never returns consumed ingredients");
    Check((await kitchen.PutAsJsonAsync($"Kitchen/{ticket}/status", new { status = "Completed" })).IsSuccessStatusCode, "Kitchen finishes remaining five drinks");
    Check((await cashier.PutAsJsonAsync($"Bill/{bill}/customer", new { idCustomer = customer })).IsSuccessStatusCode, "Attach customer before payment");
    await Rights(new { code = "POS_ORDER", allowed = false });
    Check((await cashier.PutAsJsonAsync($"Bill/{bill}/guests", new { guestCount = 2 })).StatusCode == HttpStatusCode.Forbidden, "Revoked order permission rejects guest changes immediately");
    Check((await cashier.PostAsJsonAsync($"Bill/checkout/{bill}", new { paymentMethod = "Cash", guestCount = 3, idCustomer = customer })).StatusCode == HttpStatusCode.Forbidden, "Checkout payload cannot bypass revoked guest editing permission");
    Check((await cashier.PostAsJsonAsync($"Bill/checkout/{bill}", new { paymentMethod = "Cash", guestCount = 1, idCustomer = (int?)null })).StatusCode == HttpStatusCode.Forbidden, "Checkout payload cannot remove customer without order permission");
    Check((await cashier.PostAsJsonAsync($"Bill/checkout/{bill}", new { paymentMethod = "Cash", guestCount = 1, idCustomer = customer, expectedTotal = 100000 })).StatusCode == HttpStatusCode.Conflict, "Stale payment total requires cashier review instead of silently charging different amount");
    Check(!await db.CashEntries.AnyAsync(c => c.IdBill == bill) && (await db.Customers.AsNoTracking().SingleAsync(c => c.Id == customer)).Points == 0 && (await db.Bills.AsNoTracking().SingleAsync(b => b.Id == bill)).Status == 0, "Stale payment total leaves receipt, customer points and invoice untouched");
    var payments = await Task.WhenAll(cashier.PostAsJsonAsync($"Bill/checkout/{bill}", new { paymentMethod = "Cash", guestCount = 1, idCustomer = customer }), cashier.PostAsJsonAsync($"Bill/checkout/{bill}", new { paymentMethod = "Cash", guestCount = 1, idCustomer = customer }));
    Check(payments.Count(p => p.IsSuccessStatusCode) == 1 && payments.Count(p => p.StatusCode == HttpStatusCode.NotFound) == 1, "Simultaneous cash payments settle invoice exactly once");
    Check(await db.CashEntries.CountAsync(c => c.IdBill == bill && c.Direction == "In" && c.IdShift == shift && c.Amount == 125000) == 1 && (await db.Customers.AsNoTracking().SingleAsync(c => c.Id == customer)).Points == 12, "Receipt, shift and loyalty points reconcile with five drinks paid");
    await Rights(new { code = "POS_CHECKOUT", allowed = false });
    Check((await cashier.PostAsJsonAsync($"Bill/checkout/{secondBill}", new { paymentMethod = "Transfer" })).StatusCode == HttpStatusCode.Forbidden, "Revoked checkout permission applies to real serving invoice on same token");
    Check((await other.PostAsJsonAsync($"Bill/checkout/{secondBill}", new { paymentMethod = "Transfer", expectedTotal = 25000 })).StatusCode == HttpStatusCode.Conflict, "Unsent invoice with stale displayed amount rejects automatic checkout");
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 40 && (await new StockReservations(db).Held())[ingredient] == 20 && !await db.KitchenOrders.AnyAsync(o => o.IdBill == secondBill) && !await db.CashEntries.AnyAsync(c => c.IdBill == secondBill), "Rejected stale amount rolls back automatic kitchen, consumption, reservations and cash atomically");
    Check((await other.PostAsJsonAsync($"Bill/checkout/{secondBill}", new { paymentMethod = "Transfer" })).IsSuccessStatusCode, "Other cashier pays remaining two drinks and automatically reports kitchen");
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 20 && !(await new StockReservations(db).Held()).Any(), "All serving reservations clear and remaining physical stock is twenty ml");
    var otherDetail = (await other.GetFromJsonAsync<JsonObject>($"Shift/{secondShift}"))!;
    Check(otherDetail["totals"]!["expectedCash"]!.GetValue<decimal>() == 50000, "Transfer receipt never increases cash drawer balance");
    var detail = (await cashier.GetFromJsonAsync<JsonObject>($"Shift/{shift}"))!;
    Check(detail["totals"]!["expectedCash"]!.GetValue<decimal>() == 325000, "Cash shift balance equals opening money plus recorded sale");
    Check((await cashier.PostAsJsonAsync($"Shift/{shift}/close", new { expectedCash = 200000, countedCash = 325000 })).StatusCode == HttpStatusCode.BadRequest, "Stale shift estimate cannot close after sale");
    Check((await cashier.PostAsJsonAsync($"Shift/{shift}/close", new { expectedCash = 325000, countedCash = 324000 })).StatusCode == HttpStatusCode.BadRequest, "Cash variance needs a reason");
    Check((await cashier.PostAsJsonAsync($"Shift/{shift}/close", new { expectedCash = 325000, countedCash = 325000 })).IsSuccessStatusCode, "Cashier closes matching shift");
    await Rights(new { code = "ORDERS_VIEW", allowed = true }, new { code = "ORDERS_REFUND", allowed = true });
    var refundShift = (await Post(cashier, "Shift", new { openingCash = 200000, requestKey = Guid.NewGuid().ToString() }))["id"]!.GetValue<int>();
    var refundBody = new { reason = "Workflow refund", paymentMethod = "Cash" };
    Check((await cashier.PostAsJsonAsync($"Orders/{bill}/refund", refundBody)).IsSuccessStatusCode && (await cashier.PostAsJsonAsync($"Orders/{bill}/refund", refundBody)).IsSuccessStatusCode, "Refund can retry safely");
    Check(await db.CashEntries.CountAsync(c => c.IdBill == bill && c.Direction == "Out" && c.IdShift == refundShift && c.Amount == 125000) == 1 && (await db.Customers.AsNoTracking().SingleAsync(c => c.Id == customer)).Points == 0, "Later refund creates one expense in new shift and reverses earned points");
    var closed = (await cashier.GetFromJsonAsync<JsonObject>($"Shift/{shift}"))!;
    var refundDetail = (await cashier.GetFromJsonAsync<JsonObject>($"Shift/{refundShift}"))!;
    Check(closed["totals"]!["expectedCash"]!.GetValue<decimal>() == 325000 && refundDetail["totals"]!["expectedCash"]!.GetValue<decimal>() == 75000, "Later refund leaves closed snapshot unchanged");
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i => i.Id == ingredient)).Quantity == 20, "Refund does not restore consumed stock");
    var day = DateTime.Today.ToString("yyyy-MM-dd");
    var report = (await admin.GetFromJsonAsync<JsonObject>($"Report/summary?fromDate={day}&toDate={day}"))!;
    Check(report["sales"]!["sales"]!.GetValue<decimal>() == 175000 && report["sales"]!["refunds"]!.GetValue<decimal>() == 125000 && report["sales"]!["netRevenue"]!.GetValue<decimal>() == 50000, "Financial report reconciles sales and refund across both cashiers");
    var debt = report["suppliers"]!.AsArray().OfType<JsonObject>().Single(s => s["supplierId"]!.GetValue<int>() == supplier);
    Check(debt["total"]!.GetValue<decimal>() == 10000 && debt["paid"]!.GetValue<decimal>() == 4000 && debt["debt"]!.GetValue<decimal>() == 6000, "Supplier debt reconciles imported stock and real cash payment");
    Check((await cashier.GetAsync("Report/summary?fromDate=" + day + "&toDate=" + day)).StatusCode == HttpStatusCode.Forbidden, "Cashier cannot read management report without permission");
    Check((await cashier.PutAsJsonAsync($"Bill/{bill}/guests", new { guestCount = 8 })).StatusCode == HttpStatusCode.NotFound && (await cashier.PutAsJsonAsync($"Bill/{bill}/customer", new { idCustomer = (int?)null })).StatusCode == HttpStatusCode.NotFound, "Closed invoice metadata cannot be edited");
    var raceBill = (await Post(cashier, "Bill/add-item", new { idTable = tables[0], idFood = food, count = 1 }))["idBill"]!.GetValue<int>();
    Task<HttpResponseMessage> guestEdit, customerEdit, racePayment;
    await using (var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)) {
        await new StockReservations(db).Lock();
        guestEdit = cashier.PutAsJsonAsync($"Bill/{raceBill}/guests", new { guestCount = 3 });
        customerEdit = cashier.PutAsJsonAsync($"Bill/{raceBill}/customer", new { idCustomer = customer });
        racePayment = cashier.PostAsJsonAsync($"Bill/checkout/{raceBill}", new { guestCount = 1, idCustomer = customer, paymentMethod = "Cash" });
        await Task.Delay(350);
        Check(!guestEdit.IsCompleted && !customerEdit.IsCompleted && !racePayment.IsCompleted, "Guest and customer edits obey the same transaction lock as payment");
        await tx.CommitAsync();
    }
    var edits = await Task.WhenAll(guestEdit, customerEdit);
    Check(edits.All(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NotFound) && (await racePayment).IsSuccessStatusCode, "Concurrent metadata edits and payment serialize without server errors");
    var finalInvoice = await db.Bills.AsNoTracking().SingleAsync(b => b.Id == raceBill);
    Check(finalInvoice.Status == 1 && finalInvoice.GuestCount == 1 && finalInvoice.IdCustomer == customer, "Edits cannot overwrite paid invoice metadata after checkout commits");
    Console.WriteLine($"All {checks} workflow checks passed.");
} finally {
    if (server is { HasExited: false }) { server.Kill(true); await server.WaitForExitAsync(); }
    server?.Dispose(); await db.Database.EnsureDeletedAsync();
}
