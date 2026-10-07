using CafeManagement.API.Controllers;
using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
int count = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); count++; }
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE")) { InitialCatalog = "CafeStaffChecks_" + Guid.NewGuid().ToString("N") };
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
var identity = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"admin"),new Claim(ClaimTypes.Role,"Admin")],"test"));
var ctx = new ControllerContext { HttpContext = new DefaultHttpContext { User = identity } };
try {
 await DbSeeder.SeedAsync(db);
 Check(!db.Database.HasPendingModelChanges(),"Migration matches model");
 Check(await db.Employees.CountAsync() == 1,"Existing admin receives employee profile");
 var staff = new EmployeeController(db) { ControllerContext = ctx }; var accounts = new AccountController(db) { ControllerContext = ctx };
 Check(await staff.Create(new() {Name="", Phone="123"}) is BadRequestObjectResult,"Empty name rejected");
 Check(await staff.Create(new() {Name="A", Email="wrong"}) is BadRequestObjectResult,"Invalid email rejected");
 Check(await staff.Create(new() {Name="A", Birthday=DateTime.Today.AddDays(1)}) is BadRequestObjectResult,"Future birthday rejected");
 Check(await staff.Create(new() {Name="  Test worker  ", Department="Phục vụ", HireDate=DateTime.Today}) is OkObjectResult,"Create profile without login");
 var e = await db.Employees.SingleAsync(x => x.Name == "Test worker");
 Check(e.UserName == null,"No login required");
 var cashier = await db.Roles.SingleAsync(x => x.Name=="Cashier");
 Check(await staff.Login(e.Id,new() {UserName="test.worker",PassWord="short",IdRole=cashier.Id}) is BadRequestObjectResult,"Short password rejected");
 Check(await staff.Login(e.Id,new() {UserName="test.worker",PassWord="StaffDemo123",IdRole=cashier.Id}) is OkResult,"Atomic login creation and link");
 Check(await staff.Login(e.Id,new() {UserName="other",PassWord="StaffDemo123",IdRole=cashier.Id}) is BadRequestObjectResult,"Second account blocked");
 Check(await accounts.UpdateStatus("admin",new() {IsActive=false}) is BadRequestObjectResult,"Self lock blocked");
 Check(await accounts.Update("admin",new() {DisplayName="Admin",IdRole=cashier.Id}) is BadRequestObjectResult,"Self demotion blocked");
 Check(await AccountController.Guard(db,await db.Accounts.FindAsync("admin") ?? throw new Exception(),"other",true) != null,"Last admin protected");
 var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Jwt:Key"]="test-test-test-test-test-test-test-test-test-test",["Jwt:Issuer"]="test",["Jwt:Audience"]="test" }).Build();
 var auth = new AuthController(db,config);
 Check(await auth.Login(new() {UserName="test.worker",PassWord="StaffDemo123"}) is OkObjectResult,"New account can log in");
 Check(await staff.Status(e.Id,new() {IsActive=false}) is OkResult,"Retirement locks account");
 Check(await auth.Login(new() {UserName="test.worker",PassWord="StaffDemo123"}) is UnauthorizedObjectResult,"Locked account cannot log in");
 Check(await accounts.UpdateStatus("test.worker",new() {IsActive=true}) is BadRequestObjectResult,"Cannot open account while retired");
 Check(await staff.Status(e.Id,new() {IsActive=true}) is OkResult,"Rehire works");
 Check(!(await db.Accounts.FindAsync("test.worker"))!.IsActive,"Rehire does not automatically open login");
 Check(await accounts.UpdateStatus("test.worker",new() {IsActive=true}) is OkResult,"Explicit unlock works");
 var version = (await db.Accounts.FindAsync("test.worker"))!.SecurityVersion;
 Check(await accounts.Update("test.worker",new() {DisplayName="Worker",IdRole=cashier.Id,PassWord="ResetDemo123"}) is OkResult,"Password reset works");
 Check((await db.Accounts.FindAsync("test.worker"))!.SecurityVersion > version,"Password reset revokes old session");
 Check(await auth.Login(new() {UserName="test.worker",PassWord="StaffDemo123"}) is UnauthorizedObjectResult,"Old password rejected");
 Check(await auth.Login(new() {UserName="test.worker",PassWord="ResetDemo123"}) is OkObjectResult,"New password accepted");
 Check(accounts.Delete("test.worker") is BadRequestObjectResult && await db.Accounts.AnyAsync(a=>a.UserName=="test.worker"),"Historical account preserved");
 Check(await db.EmployeeActivities.CountAsync(x=>x.EmployeeId==e.Id)>=6,"Audit history recorded");
 Check(AccountController.PasswordError(new string('é',40)) != null,"BCrypt byte limit enforced");
 var upgradeCs = new SqlConnectionStringBuilder(cs.ConnectionString) { InitialCatalog = cs.InitialCatalog + "Upgrade" };
 await using var upgrade = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(upgradeCs.ConnectionString).Options);
 try {
     var previous = upgrade.Database.GetMigrations().TakeWhile(m => !m.EndsWith("_EmployeeManagement")).Last();
     await upgrade.GetService<IMigrator>().MigrateAsync(previous);
     await upgrade.Database.ExecuteSqlRawAsync("INSERT INTO Role (Name) VALUES ('Admin');");
     var hash = BCrypt.Net.BCrypt.HashPassword("LegacyAdmin123");
     await upgrade.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Account(UserName,PassWord,DisplayName,IdRole,IsActive) VALUES ('legacy.admin',{hash},'Legacy',1,0)");
     await upgrade.Database.MigrateAsync();
     var recovered = await upgrade.Accounts.SingleAsync();
     Check(recovered.IsActive, "Upgrade restores one admin when all legacy admins are locked");
     Check(recovered.PassWord == hash, "Upgrade keeps existing password hash");
     Check(await upgrade.Employees.AnyAsync(x => x.UserName == "legacy.admin" && x.IsActive), "Upgrade backfills active linked employee");
 } finally { await upgrade.Database.EnsureDeletedAsync(); }
 Console.WriteLine($"{count} staff checks passed");
} finally { await db.Database.EnsureDeletedAsync(); }

