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
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE")) { InitialCatalog = "CafeQrOrderingChecks_" + Guid.NewGuid().ToString("N") };
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

    using var guest = new HttpClient { BaseAddress = address };
    var table = await db.TableFoods.OrderBy(t=>t.Id).Select(t=>t.Id).FirstAsync();
    var table2 = await db.TableFoods.OrderBy(t=>t.Id).Select(t=>t.Id).Skip(1).FirstAsync();
    var category = await db.FoodCategories.Select(c=>c.Id).FirstAsync();
    var milk = new Ingredient {Name="QR milk",Unit="ml",Quantity=100,UnitCost=100}; db.Ingredients.Add(milk);await db.SaveChangesAsync();
    db.StockLots.Add(new StockLot {IdIngredient=milk.Id,Code="QR-FRESH",Quantity=100,UnitCost=100,ExpiryDate=DateTime.Today.AddDays(5)});
    var plain = new Food {Name="QR latte",Price=25000,IdCategory=category};
    var topping = new Food {Name="QR cream",Price=5000,IdCategory=category,IsTopping=true};
    var heavy = new Food {Name="QR heavy",Price=50000,IdCategory=category};
    db.Foods.AddRange(plain,topping,heavy);await db.SaveChangesAsync();
    var size = new FoodVariant {IdFood=plain.Id,Name="L",Price=30000};db.FoodVariants.Add(size);await db.SaveChangesAsync();
    db.Recipes.AddRange(new Recipe {IdFood=plain.Id,IdIngredient=milk.Id,Amount=10},new Recipe {IdFood=plain.Id,IdVariant=size.Id,IdIngredient=milk.Id,Amount=10},new Recipe {IdFood=topping.Id,IdIngredient=milk.Id,Amount=2},new Recipe {IdFood=heavy.Id,IdIngredient=milk.Id,Amount=95});
    db.FoodToppings.Add(new FoodTopping {IdFood=plain.Id,IdTopping=topping.Id});await db.SaveChangesAsync();
    object Item(int food,int count=1,int? variant=null,bool cream=false) => new {idFood=food,count,idVariant=variant,toppings=cream?new[]{new{idFood=topping.Id,count=1}}:Array.Empty<object>()};
    object Body(string key,decimal total, params object[] items)=> new {requestKey=key,expectedTotal=total,note="Ít đá",items};
    var key=Guid.NewGuid().ToString();var body=Body(key,70000,Item(plain.Id,2,size.Id,true));
    var menu=(await guest.GetFromJsonAsync<JsonObject>($"PublicMenu/{table}"))!;
    Check(menu["foods"]!.AsArray().OfType<JsonObject>().Single(f=>f["id"]!.GetValue<int>()==plain.Id)["toppings"]![0]!["id"]!.GetValue<int>()==topping.Id,"Public menu provides selectable variant and topping IDs");
    var attempts=await Task.WhenAll(Enumerable.Range(0,3).Select(_=>guest.PostAsJsonAsync($"PublicMenu/{table}/requests",body)));
    Check(attempts.All(r=>r.IsSuccessStatusCode),"Concurrent guest submit retries succeed");
    var submitted=(await attempts[0].Content.ReadFromJsonAsync<JsonObject>())!;var id=submitted["id"]!.GetValue<int>();
    Check(await db.QrOrderRequests.CountAsync(r=>r.RequestKey==key)==1,"One request persisted for repeated guest key");
    Check(!(await new StockReservations(db).Held()).ContainsKey(milk.Id)&&!await db.Bills.AnyAsync(b=>b.IdTable==table&&b.Status==0),"Pending guest request reserves nothing and creates no bill");
    Check((await guest.GetAsync($"PublicMenu/{table}/requests/{Guid.NewGuid()}")).StatusCode==HttpStatusCode.NotFound,"Unrelated key cannot read another guest request");
    Check((await guest.GetAsync($"PublicMenu/{table2}/requests/{key}")).StatusCode==HttpStatusCode.NotFound,"Tracking capability is scoped to table");
    Check((await guest.GetAsync("QrOrders")).StatusCode==HttpStatusCode.Unauthorized,"Anonymous cannot read cashier inbox");
    Check((await kitchen.GetAsync("QrOrders")).StatusCode==HttpStatusCode.Forbidden,"Kitchen role cannot read guest queue");
    Check((await kitchen.PostAsJsonAsync($"QrOrders/{id}/decision",new{accept=true})).StatusCode==HttpStatusCode.Forbidden,"Kitchen cannot approve guest requests");
    Check((await guest.PostAsJsonAsync($"PublicMenu/{table}/requests",Body(Guid.NewGuid().ToString(),1,Item(plain.Id,1,size.Id)))).StatusCode==HttpStatusCode.BadRequest,"Server rejects tampered price");
    Check((await guest.PostAsJsonAsync($"PublicMenu/{table}/requests",Body(Guid.NewGuid().ToString(),30000,Item(plain.Id)))).StatusCode==HttpStatusCode.BadRequest,"Missing required size rejected");
    var decisions=await Task.WhenAll(cashier.PostAsJsonAsync($"QrOrders/{id}/decision",new{accept=true}),other.PostAsJsonAsync($"QrOrders/{id}/decision",new{accept=true}));
    Check(decisions.All(r=>r.IsSuccessStatusCode),"Two cashiers approve idempotently");
    var bill=await db.QrOrderRequests.AsNoTracking().Where(r=>r.Id==id).Select(r=>r.IdBill).SingleAsync();
    Check(await db.BillInfos.Where(i=>i.IdBill==bill).SumAsync(i=>i.Count)==2,"Concurrent approvals add requested count once");
    var line=await db.BillInfos.AsNoTracking().SingleAsync(i=>i.IdBill==bill);
    Check(line.UnitPrice==35000&&line.OptionLabel.Contains("Ít đá")&&line.OptionLabel.Contains("QR cream"),"Size topping price and guest note preserved on bill");
    Check((await new StockReservations(db).Held())[milk.Id]==24&&(await db.Ingredients.AsNoTracking().SingleAsync(i=>i.Id==milk.Id)).Quantity==100,"Approval reserves recipe including toppings without consuming stock");
    Check((await guest.GetFromJsonAsync<JsonObject>($"PublicMenu/{table}/requests/{key}"))!["status"]!.GetValue<string>()=="Accepted","Guest sees accepted status");
    var failKey=Guid.NewGuid().ToString();var fail=(await Post(guest,$"PublicMenu/{table2}/requests",Body(failKey,80000,Item(plain.Id,1,size.Id),Item(heavy.Id))))["id"]!.GetValue<int>();
    Check((await cashier.PostAsJsonAsync($"QrOrders/{fail}/decision",new{accept=true})).StatusCode==HttpStatusCode.BadRequest,"Insufficient stock blocks whole approval batch");
    Check(!await db.Bills.AnyAsync(b=>b.IdTable==table2&&b.Status==0)&&await db.QrOrderRequests.AsNoTracking().AnyAsync(r=>r.Id==fail&&r.Status=="Pending"),"Failed batch rolls back first item bill and request remains pending");
    Check((await new StockReservations(db).Held())[milk.Id]==24,"Failed approval creates no extra reservation");
    Check((await cashier.PostAsJsonAsync($"QrOrders/{fail}/decision",new{accept=false})).StatusCode==HttpStatusCode.BadRequest,"Reject requires visible guest reason");
    await Post(cashier,$"QrOrders/{fail}/decision",new{accept=false,reason="Hết sữa, vui lòng chọn món khác"});
    Check((await guest.GetFromJsonAsync<JsonObject>($"PublicMenu/{table2}/requests/{failKey}"))!["reason"]!.GetValue<string>().Contains("Hết sữa"),"Guest sees rejection reason");
    Check((await cashier.PostAsJsonAsync($"QrOrders/{fail}/decision",new{accept=true})).StatusCode==HttpStatusCode.BadRequest,"Rejected request cannot later be accepted");
    var stale=(await Post(guest,$"PublicMenu/{table2}/requests",Body(Guid.NewGuid().ToString(),30000,Item(plain.Id,1,size.Id))))["id"]!.GetValue<int>();
    await db.FoodVariants.Where(v=>v.Id==size.Id).ExecuteUpdateAsync(s=>s.SetProperty(v=>v.Price,31000));
    Check((await cashier.PostAsJsonAsync($"QrOrders/{stale}/decision",new{accept=true})).StatusCode==HttpStatusCode.BadRequest,"Menu price changes require guest reconfirmation");
    await db.QrOrderRequests.Where(r=>r.Id==stale).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.ExpiresAt,DateTime.UtcNow.AddMinutes(-1)));
    Check((await cashier.PostAsJsonAsync($"QrOrders/{stale}/decision",new{accept=true})).StatusCode==HttpStatusCode.BadRequest,"Expired request cannot be approved into a new table visit");
    var pending=await cashier.GetFromJsonAsync<JsonArray>("QrOrders");Check(!pending!.Any(r=>r!["id"]!.GetValue<int>()==stale),"Expired request removed from pending inbox");
    var ticket=await Post(cashier,"Kitchen/send-order",new{idBill=bill});
    Check((await db.Ingredients.AsNoTracking().SingleAsync(i=>i.Id==milk.Id)).Quantity==76,"Normal report to kitchen consumes accepted QR recipes once");
    var detail=await db.KitchenOrderDetails.AsNoTracking().SingleAsync(d=>d.IdKitchenOrder==ticket["idKitchenOrder"]!.GetValue<int>());
    Check(detail.OptionLabel.Contains("Ít đá"),"Kitchen receives guest preparation note");
    Check((await cashier.PostAsJsonAsync($"QrOrders/{id}/decision",new{accept=true})).IsSuccessStatusCode&&(await db.Ingredients.AsNoTracking().SingleAsync(i=>i.Id==milk.Id)).Quantity==76,"Approval retry after kitchen reporting never adds or consumes again");
    var limited=false;for(var n=0;n<25;n++){var r=await guest.PostAsJsonAsync($"PublicMenu/{table}/requests",body);if(r.StatusCode==HttpStatusCode.TooManyRequests){limited=true;break;}}
    Check(limited,"Guest submission rate limit bounds request spam");
    var independent = await Post(guest,$"PublicMenu/{table2}/requests",Body(Guid.NewGuid().ToString(),31000,Item(plain.Id,1,size.Id)));
    Check(independent["status"]!.GetValue<string>()=="Pending","Rate limit at one table does not block another table through frontend proxy");
    await db.TableFoods.Where(t=>t.Id==table2).ExecuteUpdateAsync(s=>s.SetProperty(t=>t.IsActive,false));
    Check((await guest.GetAsync($"PublicMenu/{table2}")).StatusCode==HttpStatusCode.NotFound,"Stopped table cannot serve public QR ordering");
    Check((await cashier.PostAsJsonAsync($"QrOrders/{independent["id"]!.GetValue<int>()}/decision",new{accept=true})).StatusCode==HttpStatusCode.BadRequest,"Table stopped after submission cannot receive approved items");
    Check(!(await new StockReservations(db).Held()).ContainsKey(milk.Id),"Inactive table approval leaves stock reservation unchanged");
    Console.WriteLine($"All {checks} QR ordering checks passed.");
} finally {
    if (server is { HasExited: false }) { server.Kill(true); await server.WaitForExitAsync(); }
    server?.Dispose(); await db.Database.EnsureDeletedAsync();
}
