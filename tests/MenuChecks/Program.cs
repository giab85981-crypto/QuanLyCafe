using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using CafeManagement.API.DTOs;
using CafeManagement.API.Controllers;
using CafeManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
static void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
static async Task Reject(Func<Task> action, string name) { try { await action(); } catch (InvalidOperationException) { Console.WriteLine("PASS: " + name); return; } throw new Exception("FAIL: " + name); }
var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE") ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;") { InitialCatalog = "CafeMenuChecks_" + Guid.NewGuid().ToString("N") };
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
try
{
    await db.Database.MigrateAsync();
    var category = new FoodCategory { Name = "Test drinks" }; var ingredient = new Ingredient { Name = "Milk", Unit = "ml", Quantity = 100, UnitCost = 10 };
    var table = new TableFood { Name = "Test table", IsActive = true }; db.AddRange(category, ingredient, table); await db.SaveChangesAsync();
    var catalog = new MenuCatalog(db);
    var topping = new Food(); db.Foods.Add(topping);
    await catalog.Save(topping, new CreateFoodDto { Name = "Cream", IdCategory = category.Id, IsTopping = true, Price = 5000, Recipe = [new() { IdIngredient = ingredient.Id, Amount = 5 }] });
    var food = new Food(); db.Foods.Add(food);
    var write = new CreateFoodDto { Name = "Latte", IdCategory = category.Id, Price = 25000, ToppingIds = [topping.Id], Variants = [new() { Name = "L", Price = 40000, Recipe = [new() { IdIngredient = ingredient.Id, Amount = 20 }] }] };
    Check(await catalog.Validate(write) == null, "Valid size/topping/recipe configuration"); await catalog.Save(food, write);
    db.ChangeTracker.Clear(); food = await catalog.Query().SingleAsync(f => f.Id == food.Id); var variantId = food.Variants.Single().Id;
    Check(MenuCatalog.Dto(food).Variants.Single().CostPrice == 200 && MenuCatalog.Dto(food).AvailableQuantity == 5, "Calculated cost and availability by size");
    var flow = new OrderFlow(db);
    await Reject(async () => { await flow.Add(new() { IdTable = table.Id, IdFood = food.Id, Count = 1 }); }, "Size required"); db.ChangeTracker.Clear();
    var billId = await flow.Add(new() { IdTable = table.Id, IdFood = food.Id, IdVariant = variantId, Count = 2, Toppings = [new() { IdFood = topping.Id, Count = 2 }] });
    var line = await db.BillInfos.SingleAsync(i => i.IdBill == billId);
    Check(line.UnitPrice == 50000 && line.CostPrice == 300 && line.SentCount == 0, "Size + two toppings snapshot price/cost");
    var controller = new BillController(db); Check(await controller.Checkout(billId, new() { PaymentMethod = "Invalid" }) is BadRequestObjectResult, "Invalid checkout preserves unsent reservation");
    var variant = await db.FoodVariants.FindAsync(variantId); variant!.Price = 99999; var recipe = await db.Recipes.SingleAsync(r => r.IdVariant == variantId); recipe.Amount = 99; await db.SaveChangesAsync();
    var orderId = await flow.Send(billId); db.ChangeTracker.Clear();
    Check((await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 40, "Kitchen uses saved recipe even after menu change");
    Check(await flow.Send(billId) == null && await db.StockMovements.CountAsync() == 1, "Repeat kitchen send is idempotent");
    await flow.Cancel(line.Id, new() { Count = 1, Reason = "Customer changed mind" }); db.ChangeTracker.Clear();
    Check((await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 70 && (await db.BillInfos.FindAsync(line.Id))!.SentCount == 1, "Pending cancellation returns only cancelled ingredients");
    var kitchen = new KitchenController(db); Check(await kitchen.Status(orderId!.Value, new() { Status = "Cooking" }) is OkResult, "Start kitchen cooking");
    await flow.Cancel(line.Id, new() { Count = 1, Reason = "Customer left" }); db.ChangeTracker.Clear();
    Check((await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 70 && await db.StockMovements.AnyAsync(m => m.Kind == "Waste" && m.Quantity == -30), "Cooking cancellation logs waste without another deduction");
    await Reject(async () => { await flow.Add(new() { IdTable = table.Id, IdFood = food.Id, IdVariant = variantId, Count = 1 }); }, "Insufficient stock blocks adding item before kitchen"); db.ChangeTracker.Clear();
    Check((await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 70 && await db.KitchenOrders.CountAsync() == 1, "Insufficient stock leaves no order or stock changes");
    ingredient = (await db.Ingredients.FindAsync(ingredient.Id))!; await new StockLots(db).Add(ingredient, 130, ingredient.UnitCost, "Count", "Test restock");
    await flow.Add(new() { IdTable = table.Id, IdFood = food.Id, IdVariant = variantId, Count = 1 }); await flow.Send(billId); db.ChangeTracker.Clear(); Check((await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 101, "Only newly added line is deducted");
    controller = new BillController(db); Check(await controller.Checkout(billId, new() { Discount = 10 }) is OkObjectResult && (await db.Bills.FindAsync(billId))!.TotalPrice == 89999.1m, "Checkout sums current line snapshots with discount");
    Check(await db.CashEntries.CountAsync(c => c.IdBill == billId) == 1 && (await db.CashEntries.SingleAsync(c => c.IdBill == billId)).Amount == 89999.1m, "Checkout records discounted actual revenue once");
    Check(await controller.Checkout(billId, new()) is NotFoundObjectResult && await db.CashEntries.CountAsync(c => c.IdBill == billId) == 1, "Checkout retry cannot duplicate cash income");
    var cashResult = (OkObjectResult)await new CashbookController(db).Get(DateTime.Today, DateTime.Today);
    Check(System.Text.Json.JsonSerializer.SerializeToElement(cashResult.Value).GetProperty("TotalIn").GetDecimal() == 89999.1m, "Cashbook does not double count recorded sales as legacy bills");
    var foodController = new FoodController(db); Check(await foodController.Delete(food.Id) is OkObjectResult && (await db.Foods.FindAsync(food.Id))!.IsActive == false, "Deleting sold food deactivates and preserves history");
    await Reject(async () => { await flow.Add(new() { IdTable = table.Id, IdFood = food.Id, IdVariant = variantId, Count = 1 }); }, "Inactive food cannot be ordered"); db.ChangeTracker.Clear();
    var imported = await foodController.Import(new() { Preview = false, Items = [new() { Name = "Bad", IdCategory = category.Id, Recipe = [new() { IdIngredient = ingredient.Id, Amount = -1 }] }] });
    Check(imported is OkObjectResult && !await db.Foods.AnyAsync(f => f.Name == "Bad"), "Import validation prevents partial writes");
    Console.WriteLine("All menu workflow checks passed.");
}
finally { await db.Database.EnsureDeletedAsync(); }
