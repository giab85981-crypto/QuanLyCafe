using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
int count = 0;
void Check(bool ok, string text) { if (!ok) throw new Exception(text); Console.WriteLine("PASS " + text); count++; }
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE")) { InitialCatalog = "CafeAccessChecks_" + Guid.NewGuid().ToString("N") };
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
if (args.Length == 2 && args[0] is "--seed-preview" or "--delete-preview") {
 if (!System.Text.RegularExpressions.Regex.IsMatch(args[1], @"^CafeAccessPreview_[a-f0-9]{32}$")) throw new Exception("Preview database name rejected");
 var previewCs = new SqlConnectionStringBuilder(cs.ConnectionString) { InitialCatalog = args[1] };
 await using var preview = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(previewCs.ConnectionString).Options);
 if (args[0] == "--delete-preview") { await preview.Database.EnsureDeletedAsync(); Console.WriteLine("Preview database deleted"); return; }
 await DbSeeder.SeedAsync(preview); var role = await preview.Roles.SingleAsync(r => r.Name == "Cashier");
 preview.Accounts.Add(new() { UserName="access.worker", DisplayName="Nhân viên thử phân quyền", IdRole=role.Id, PassWord=BCrypt.Net.BCrypt.HashPassword("DemoAccess123") }); await preview.SaveChangesAsync(); Console.WriteLine("Preview database seeded"); return;
}
Process? server = null;
try {
 await DbSeeder.SeedAsync(db); Check(!db.Database.HasPendingModelChanges(), "Migration matches permission model");
 var cashier = await db.Roles.SingleAsync(r => r.Name == "Cashier"); var kitchen = await db.Roles.SingleAsync(r => r.Name == "Kitchen");
 db.Accounts.Add(new() { UserName="access.worker", DisplayName="Access worker", PassWord=BCrypt.Net.BCrypt.HashPassword("DemoAccess123"), IdRole=cashier.Id }); await db.SaveChangesAsync();
 Check((await DynamicAccess.Effective(db,"access.worker")).Contains("POS_CHECKOUT"), "Legacy cashier seeded once with usable permissions");
 Check(!(await DynamicAccess.Effective(db,"access.worker")).Contains("KITCHEN_VIEW"), "Legacy cashier does not enter kitchen");
 Check(DynamicAccess.Normalize(["POS_DISCOUNT"]).Count == 0, "Operations without prerequisites removed");
 var listener = new TcpListener(IPAddress.Loopback,0); listener.Start(); var port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
 var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
 var psi = new ProcessStartInfo("dotnet") { WorkingDirectory=root, UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true };
 psi.ArgumentList.Add(Path.Combine(root,"CafeManagement.API/bin/Release/net8.0/CafeManagement.API.dll")); psi.ArgumentList.Add("--urls"); psi.ArgumentList.Add($"http://localhost:{port}"); psi.ArgumentList.Add("--contentRoot"); psi.ArgumentList.Add(Path.Combine(root,"CafeManagement.API"));
 psi.Environment["ConnectionStrings__DefaultConnection"]=cs.ConnectionString; psi.Environment["LocalDb__UseNamedPipe"]="false"; psi.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
 const string key="AccessTestOnlySecretKey_2026_12345678901234567890";
 psi.Environment["Jwt__Key"]=key; psi.Environment["Jwt__Issuer"]="access-tests"; psi.Environment["Jwt__Audience"]="access-tests"; psi.Environment["Logging__LogLevel__Default"]="Error";
 server=Process.Start(psi)!; var log=new StringBuilder(); server.OutputDataReceived+=(_,e)=> { if(e.Data!=null) lock(log) log.AppendLine(e.Data); }; server.ErrorDataReceived+=(_,e)=> { if(e.Data!=null) lock(log) log.AppendLine(e.Data); }; server.BeginOutputReadLine(); server.BeginErrorReadLine();
 using var admin = new HttpClient { BaseAddress=new Uri($"http://localhost:{port}/api/") }; using var worker=new HttpClient { BaseAddress=admin.BaseAddress };
 HttpResponseMessage? login=null;
 for (int i=0;i<100;i++) { if(server.HasExited) throw new Exception(log.ToString()); try { login=await admin.PostAsJsonAsync("Auth/login",new {userName="admin",passWord="admin123"}); break; } catch(HttpRequestException) {await Task.Delay(100);} }
 Check(login?.IsSuccessStatusCode==true,"API starts on isolated test database");
 Check((await admin.GetAsync("/swagger/v1/swagger.json")).IsSuccessStatusCode,"Swagger definition loads with permission endpoints"); var adminToken=(await login!.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>(); admin.DefaultRequestHeaders.Authorization=new("Bearer",adminToken);
 var response=await worker.PostAsJsonAsync("Auth/login",new {userName="access.worker",passWord="DemoAccess123"}); Check(response.IsSuccessStatusCode,"Employee logs in"); var workerToken=(await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>(); worker.DefaultRequestHeaders.Authorization=new("Bearer",workerToken);
 async Task<JsonObject> Snapshot() => (await admin.GetFromJsonAsync<JsonObject>("Access"))!;
 async Task<JsonObject> UserRow() => (await Snapshot())["accounts"]!.AsArray().OfType<JsonObject>().Single(x=>x["userName"]!.GetValue<string>()=="access.worker");
 async Task<HttpResponseMessage> Rights(int role, object[] overrides, string? revision=null) { var user=await UserRow(); return await admin.PutAsJsonAsync("Access/accounts/access.worker",new {idRole=role,overrides,revision=revision??user["revision"]!.GetValue<string>()}); }
 Check((await worker.GetAsync("Access")).StatusCode==HttpStatusCode.Forbidden,"Non-admin cannot open permission API");
 Check((await worker.GetAsync("Kitchen/pending-orders")).StatusCode==HttpStatusCode.Forbidden,"Kitchen denied before grant");
 Check((await worker.GetAsync("Food")).IsSuccessStatusCode,"POS reads menu dependency");
 Check((await worker.PostAsJsonAsync("Food",new {})).StatusCode==HttpStatusCode.Forbidden,"Direct create request blocked without MENU_CREATE");
 response=await Rights(cashier.Id,[new {code="KITCHEN_VIEW",allowed=true}]); Check(response.IsSuccessStatusCode,"Admin grants employee kitchen view override");
 Check((await worker.GetAsync("Kitchen/pending-orders")).IsSuccessStatusCode,"Same JWT sees newly granted kitchen right");
 Check((await worker.PutAsJsonAsync("Kitchen/999/status",new {status="Cooking"})).StatusCode==HttpStatusCode.Forbidden,"View-only kitchen cannot update");
 var before=await UserRow(); var stale=before["revision"]!.GetValue<string>();
 response=await Rights(cashier.Id,[]); Check(response.IsSuccessStatusCode,"Admin resets employee to inherited group");
 Check((await worker.GetAsync("Kitchen/pending-orders")).StatusCode==HttpStatusCode.Forbidden,"Revocation enforced immediately on same JWT");
 Check((await Rights(cashier.Id,[],stale)).StatusCode==HttpStatusCode.Conflict,"Stale account edits rejected");
 response=await Rights(cashier.Id,[new {code="POS_CHECKOUT",allowed=false}]); Check(response.IsSuccessStatusCode,"Explicit employee denial saved");
 Check((await worker.PostAsJsonAsync("Bill/checkout/999",new {paymentMethod="Cash",discount=0})).StatusCode==HttpStatusCode.Forbidden,"Employee denial beats group checkout grant");
 Check(!(await worker.GetFromJsonAsync<JsonObject>("Auth/me"))!["permissions"]!.AsArray().Any(x=>x!.GetValue<string>()=="POS_DISCOUNT"),"Revoking checkout also removes dependent discount");
 Check((await Rights(cashier.Id,[new {code="POS_DISCOUNT",allowed=false}])).IsSuccessStatusCode,"Revoke discount alone");
 Check((await worker.PostAsJsonAsync("Bill/checkout/999",new {paymentMethod="Cash",discount=10})).StatusCode==HttpStatusCode.Forbidden,"Checkout payload cannot bypass discount permission");
 Check((await Rights(cashier.Id,[new {code="INVENTORY_VIEW",allowed=true},new {code="INVENTORY_EXPORT",allowed=true}])).IsSuccessStatusCode,"Grant export only");
 Check((await worker.PostAsJsonAsync("Warehouse/documents",new {kind="Disposal",note="Should fail",items=Array.Empty<object>()})).StatusCode==HttpStatusCode.Forbidden,"Export permission cannot bypass disposal via payload");
 Check((await Rights(kitchen.Id,[])).IsSuccessStatusCode,"Change group without revoking session");
 Check((await worker.GetAsync("Kitchen/pending-orders")).IsSuccessStatusCode && (await worker.GetAsync("Food")).StatusCode==HttpStatusCode.Forbidden,"Same token uses new group not old role claim");
 Check((await Rights(kitchen.Id,[new {code="UNKNOWN_PERMISSION",allowed=true}])).StatusCode==HttpStatusCode.BadRequest,"Unknown permission rejected");
 var create=await admin.PostAsJsonAsync("Access/roles",new {name="Demo limited",permissions=new[]{"MENU_VIEW"}}); Check(create.IsSuccessStatusCode,"Custom group created"); var custom=(await create.Content.ReadFromJsonAsync<JsonObject>())!;
 Check((await Rights(custom["id"]!.GetValue<int>(),[])).IsSuccessStatusCode,"Assign custom group"); Check((await worker.GetAsync("Food")).IsSuccessStatusCode && (await worker.PostAsJsonAsync("Food",new {})).StatusCode==HttpStatusCode.Forbidden,"Custom view-only group works");
 var change=await admin.PutAsJsonAsync($"Access/roles/{custom["id"]}",new {name="Demo limited",permissions=Array.Empty<string>(),revision=custom["revision"]!.GetValue<string>()}); Check(change.IsSuccessStatusCode,"Clear all rights in group");
 Check((await worker.GetAsync("Food")).StatusCode==HttpStatusCode.Forbidden,"Group edit applies immediately to assigned account");
 Check((await admin.PutAsJsonAsync($"Access/roles/{custom["id"]}",new {name="Demo limited",permissions=new[]{"MENU_VIEW"},revision=custom["revision"]!.GetValue<string>()})).StatusCode==HttpStatusCode.Conflict,"Stale group edits rejected");
 await DynamicAccess.Seed(db); Check((await DynamicAccess.Effective(db,"access.worker")).Count==0,"Restart seed preserves empty custom group");
 // Exercise every private route, including writes, through the real authentication pipeline.
 // Permission denial must happen before an invalid body or a nonexistent ID is processed.
 using (var anonymous = new HttpClient { BaseAddress = admin.BaseAddress }) {
  foreach (var type in typeof(CafeManagement.API.Controllers.AuthController).Assembly.GetTypes().Where(t => t.Namespace == "CafeManagement.API.Controllers" && t.Name.EndsWith("Controller") && t.Name is not "AuthController" and not "PublicMenuController"))
  foreach (var endpoint in type.GetMethods().SelectMany(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute), true).Cast<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()))
  foreach (var verb in endpoint.HttpMethods) {
   var path = type.Name.Replace("Controller", "") + (string.IsNullOrEmpty(endpoint.Template) ? "" : "/" + System.Text.RegularExpressions.Regex.Replace(endpoint.Template, @"\{[^}]+\}", "999999"));
   using var deniedRequest = new HttpRequestMessage(new HttpMethod(verb), path) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
   using var unsignedRequest = new HttpRequestMessage(new HttpMethod(verb), path) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
   Check((await worker.SendAsync(deniedRequest)).StatusCode == HttpStatusCode.Forbidden, $"No-rights account denied {verb} {path}");
   Check((await anonymous.SendAsync(unsignedRequest)).StatusCode == HttpStatusCode.Unauthorized, $"Anonymous denied {verb} {path}");
  }
 }
 var snapshot=await Snapshot(); var adminRole=snapshot["roles"]!.AsArray().OfType<JsonObject>().Single(x=>x["name"]!.GetValue<string>()=="Admin"); var adminUser=snapshot["accounts"]!.AsArray().OfType<JsonObject>().Single(x=>x["userName"]!.GetValue<string>()=="admin");
 Check((await admin.PutAsJsonAsync($"Access/roles/{adminRole["id"]}",new {name="Admin",permissions=Array.Empty<string>(),revision=adminRole["revision"]!.GetValue<string>()})).StatusCode==HttpStatusCode.BadRequest,"Admin group immutable");
 Check((await admin.PutAsJsonAsync("Access/accounts/admin",new {idRole=kitchen.Id,overrides=Array.Empty<object>(),revision=adminUser["revision"]!.GetValue<string>()})).StatusCode==HttpStatusCode.BadRequest,"Self demotion blocked");
 Check((await admin.PutAsJsonAsync("Access/accounts/admin",new {idRole=adminRole["id"]!.GetValue<int>(),overrides=new[]{new {code="MENU_VIEW",allowed=false}},revision=adminUser["revision"]!.GetValue<string>()})).StatusCode==HttpStatusCode.BadRequest,"Admin cannot be stripped by overrides");
 Check((await DynamicAccess.Effective(db,"admin")).Count==DynamicAccess.Catalog.Length,"Admin has all catalog rights");
 Check((await Rights(cashier.Id,[new {code="STAFF_VIEW",allowed=true},new {code="STAFF_ACCOUNTS",allowed=true}])).IsSuccessStatusCode,"Delegate limited account administration");
 Check((await worker.PostAsJsonAsync("Account",new {userName="escalation",displayName="Denied",passWord="DemoAccess123",idRole=adminRole["id"]!.GetValue<int>()})).StatusCode==HttpStatusCode.Forbidden,"Delegated account creator cannot create Admin");
 var employeeId = await db.Employees.Where(e=>e.UserName=="admin").Select(e=>e.Id).SingleAsync();
 Check((await worker.PostAsJsonAsync($"Employee/{employeeId}/login",new {userName="new.admin",displayName="Denied",passWord="DemoAccess123",idRole=adminRole["id"]!.GetValue<int>()})).StatusCode!=HttpStatusCode.OK,"Employee login path does not elevate to Admin");
 Check((await worker.PutAsJsonAsync("Account/admin",new {displayName="Hacked",idRole=cashier.Id,passWord="Changed123"})).StatusCode==HttpStatusCode.Forbidden,"Delegated operator cannot reset Admin password");
 // Signed test token deliberately embeds a stale grant; server must replace it from the DB.
 var claims=new JwtSecurityTokenHandler().ReadJwtToken(workerToken).Claims.Where(c=>c.Type!="Permission").ToList(); claims.Add(new Claim("Permission","MENU_CREATE"));
 var forged=new JwtSecurityToken("access-tests","access-tests",claims,DateTime.UtcNow,DateTime.UtcNow.AddMinutes(5),new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256));
 worker.DefaultRequestHeaders.Authorization=new("Bearer",new JwtSecurityTokenHandler().WriteToken(forged));
 Check((await worker.PostAsJsonAsync("Food",new {})).StatusCode==HttpStatusCode.Forbidden,"Stale token grants cannot override live DB permissions");
 Check((await admin.GetAsync("Access/audit")).IsSuccessStatusCode && await db.AccessAudits.CountAsync()>=8,"Permission changes audited");
 Check((await worker.GetAsync("PublicMenu/1")).StatusCode!=HttpStatusCode.Forbidden,"Public QR endpoint remains public");
 // Shift authorization is checked through the real API with live DB permissions.
 response = await Rights(cashier.Id, []); Check(response.IsSuccessStatusCode, "Clear explicit rights before shift access checks");
 // A freshly seeded Cashier inherits SHIFT_SELF; revoke it explicitly for denied-access checks.
 response = await Rights(cashier.Id, [new {code="SHIFT_SELF",allowed=false}]); Check(response.IsSuccessStatusCode,"Revoke personal shift access");
 Check((await worker.GetAsync("Shift")).StatusCode == HttpStatusCode.Forbidden, "Shift history denied without personal or management permission");
 Check((await worker.PostAsJsonAsync("Shift", new {openingCash=100000,note="",requestKey=Guid.NewGuid().ToString()})).StatusCode == HttpStatusCode.Forbidden, "Shift opening requires own-shift permission");
 response = await Rights(cashier.Id, [new {code="SHIFT_SELF",allowed=true}]); Check(response.IsSuccessStatusCode, "Grant personal shift access without new login");
 var shiftKey = Guid.NewGuid().ToString();
 response = await worker.PostAsJsonAsync("Shift", new {openingCash=200000,note="Worker",requestKey=shiftKey}); Check(response.IsSuccessStatusCode,"Employee opens own shift");
 var ownShift = (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<int>();
 response = await worker.PostAsJsonAsync("Shift", new {openingCash=200000,note="Worker",requestKey=shiftKey}); Check(response.IsSuccessStatusCode && (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<int>() == ownShift, "HTTP opening retry returns same shift");
 response = await admin.PostAsJsonAsync("Shift", new {openingCash=100000,note="Admin",requestKey=Guid.NewGuid().ToString()}); Check(response.IsSuccessStatusCode,"Admin opens own shift");
 var adminShift = (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<int>();
 Check((await worker.GetAsync($"Shift/{adminShift}")).StatusCode == HttpStatusCode.Forbidden,"Employee cannot read another shift by ID");
 Check((await worker.PostAsJsonAsync($"Shift/{adminShift}/close",new {countedCash=100000,expectedCash=100000,note=""})).StatusCode == HttpStatusCode.Forbidden,"Employee cannot close another shift by ID");
 Check((await worker.GetAsync($"Shift/{ownShift}")).IsSuccessStatusCode,"Employee reads own shift");
 response = await Rights(cashier.Id,[new {code="SHIFT_SELF",allowed=true},new {code="SHIFT_VIEW",allowed=true}]); Check(response.IsSuccessStatusCode,"Grant shift management view");
 Check((await worker.GetAsync($"Shift/{adminShift}")).IsSuccessStatusCode,"Shift viewer reads other shift");
 Check((await worker.PostAsJsonAsync($"Shift/{adminShift}/close",new {countedCash=100000,expectedCash=100000,note=""})).StatusCode == HttpStatusCode.Forbidden,"Shift viewer cannot close other shift");
 response = await Rights(cashier.Id,[new {code="SHIFT_SELF",allowed=true},new {code="SHIFT_VIEW",allowed=true},new {code="SHIFT_CLOSE_OTHER",allowed=true}]); Check(response.IsSuccessStatusCode,"Grant separate shift closing permission");
 Check((await worker.PostAsJsonAsync($"Shift/{adminShift}/close",new {countedCash=100000,expectedCash=100000,note=""})).IsSuccessStatusCode,"Delegated manager closes employee shift");
 Check((await worker.PostAsJsonAsync($"Shift/{ownShift}/close",new {countedCash=199000,expectedCash=200000,note=""})).StatusCode == HttpStatusCode.BadRequest,"HTTP close rejects unexplained cash variance");
 Check((await worker.PostAsJsonAsync($"Shift/{ownShift}/close",new {countedCash=200000,expectedCash=200000,note=""})).IsSuccessStatusCode,"Employee closes own shift");
 Check((await worker.PostAsJsonAsync($"Shift/{ownShift}/close",new {countedCash=200000,expectedCash=200000,note=""})).IsSuccessStatusCode,"HTTP close retry remains idempotent");
 Check((await admin.PutAsJsonAsync("Account/access.worker/status",new {isActive=false})).IsSuccessStatusCode,"Admin locks employee login");
 Check((await worker.GetAsync("Auth/me")).StatusCode == HttpStatusCode.Unauthorized,"Locked employee token rejected immediately");
 Check((await admin.PutAsJsonAsync("Account/access.worker/status",new {isActive=true})).IsSuccessStatusCode,"Admin unlocks employee login");
 Check((await worker.GetAsync("Auth/me")).StatusCode == HttpStatusCode.Unauthorized,"Unlock never revives previously revoked token");
 response = await worker.PostAsJsonAsync("Auth/login",new {userName="access.worker",passWord="DemoAccess123"}); Check(response.IsSuccessStatusCode,"Unlocked employee signs in again");
 worker.DefaultRequestHeaders.Authorization=new("Bearer",(await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>());
 Check((await admin.PutAsJsonAsync("Account/access.worker",new {displayName="Access worker",idRole=cashier.Id,passWord="ResetAccess123"})).IsSuccessStatusCode,"Admin resets employee password");
 Check((await worker.GetAsync("Auth/me")).StatusCode == HttpStatusCode.Unauthorized,"Password reset rejects already-issued employee token");
 Check((await worker.PostAsJsonAsync("Auth/login",new {userName="access.worker",passWord="DemoAccess123"})).StatusCode == HttpStatusCode.Unauthorized,"Old password rejected after reset");
 Check((await worker.PostAsJsonAsync("Auth/login",new {userName="access.worker",passWord="ResetAccess123"})).IsSuccessStatusCode,"Reset password allows new login");
 foreach(var type in typeof(CafeManagement.API.Controllers.AuthController).Assembly.GetTypes().Where(t=>t.Name.EndsWith("Controller") && t.Namespace=="CafeManagement.API.Controllers" && t.Name is not "AuthController" and not "PublicMenuController"))
 foreach(var method in type.GetMethods().Where(m=>m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute),true).Length>0)) Check(DynamicAccess.Required(type.Name.Replace("Controller",""),method.Name,"GET").Length>0,$"Mapped {type.Name}.{method.Name}");
 Console.WriteLine($"{count} access checks passed");
} finally { if(server is {HasExited:false}) { server.Kill(true); await server.WaitForExitAsync(); } server?.Dispose(); await db.Database.EnsureDeletedAsync(); }

