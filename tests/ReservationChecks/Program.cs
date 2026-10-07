using CafeManagement.API.Controllers;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); checks++; Console.WriteLine("PASS: " + label); }
var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE") ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;") { InitialCatalog = "CafeReservationChecks_" + Guid.NewGuid().ToString("N") };
AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
await using var db = Context();
void Clear() => db.ChangeTracker.Clear();
async Task Reject(Func<Task> action, string label) { try { await action(); } catch (InvalidOperationException) { Check(true, label); Clear(); return; } throw new Exception("FAIL: " + label); }
async Task<double> Held(int id) => (await new StockReservations(db).Held()).GetValueOrDefault(id);
try
{
    await db.Database.MigrateAsync();
    var ingredient = new Ingredient { Name = "Milk", Unit = "ml", Quantity = 100, UnitCost = 1 };
    var category = new FoodCategory { Name = "Drinks" };
    var a = new TableFood { Name = "A" }; var b = new TableFood { Name = "B" }; var c = new TableFood { Name = "C" };
    db.AddRange(ingredient, category, a, b, c); await db.SaveChangesAsync();
    var food = new Food(); db.Foods.Add(food);
    await new MenuCatalog(db).Save(food, new CreateFoodDto { Name = "Latte", IdCategory = category.Id, Price = 10000, Recipe = [new() { IdIngredient = ingredient.Id, Amount = 10 }] }); Clear();
    var flow = new OrderFlow(db);
    var first = await flow.Add(new() { IdTable = a.Id, IdFood = food.Id, Count = 7 }); Clear();
    Check(await Held(ingredient.Id) == 70 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 100 && !await db.StockMovements.AnyAsync(), "Adding seven drinks reserves seventy without physical consumption");
    var catalog = await new MenuCatalog(db).Query().SingleAsync(f => f.Id == food.Id);
    Check(MenuCatalog.Dto(catalog, await new StockReservations(db).Held()).AvailableQuantity == 3, "Menu shows three remaining across all tables");
    await Reject(async () => await flow.Add(new() { IdTable = b.Id, IdFood = food.Id, Count = 4 }), "Second table cannot reserve more than remaining stock");
    Check(await db.Bills.CountAsync() == 1 && await Held(ingredient.Id) == 70, "Rejected add leaves no bill or reservation");
    var second = await flow.Add(new() { IdTable = b.Id, IdFood = food.Id, Count = 3 }); Clear();
    Check(await Held(ingredient.Id) == 100, "Reservations survive a fresh context and fill remaining stock");
    var firstLine = await db.BillInfos.SingleAsync(i => i.IdBill == first);
    await flow.Cancel(firstLine.Id, new() { Count = 2 }); Clear();
    Check(await Held(ingredient.Id) == 80 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 100, "Unsent cancellation frees reservation without stock movement");
    await flow.Send(first); Clear();
    Check(await Held(ingredient.Id) == 30 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 50, "Reporting kitchen converts own reservation and preserves other table");
    Check(await flow.Send(first) == null && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 50, "Repeated report never consumes twice");
    await flow.Add(new() { IdBill = first, IdFood = food.Id, Count = 2 }); Clear();
    Check(await Held(ingredient.Id) == 50, "Additional items reserve only their new quantities");
    var checkout = new BillController(db);
    Check(await checkout.Checkout(first, new() { RedeemPoints = 1 }) is BadRequestObjectResult, "Invalid loyalty payment rejected after attempted automatic report"); Clear();
    Check(await Held(ingredient.Id) == 50 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 50 && await db.KitchenOrders.CountAsync() == 1 && !await db.CashEntries.AnyAsync(), "Failed payment rolls back kitchen, stock, reservation and cash atomically");
    Check(await checkout.Checkout(first, new()) is OkObjectResult, "Checkout automatically reports remaining items"); Clear();
    Check(await Held(ingredient.Id) == 30 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 30 && await db.KitchenOrders.CountAsync() == 2 && (await db.CashEntries.SingleAsync()).Amount == 70000, "Checkout consumes new items once and keeps other reservation");
    Check(await checkout.Checkout(first, new()) is NotFoundObjectResult && await db.CashEntries.CountAsync() == 1, "Repeated checkout cannot double consume or receive money"); Clear();
    var secondLine = await db.BillInfos.SingleAsync(i => i.IdBill == second);
    await flow.Cancel(secondLine.Id, new() { Count = 1 }); Clear();
    var warehouse = new WarehouseFlow(db);
    await Reject(async () => await warehouse.Operate(new() { Kind = "Export", Note = "Test", Items = [new() { IdIngredient = ingredient.Id, Quantity = 15 }] }, "tester"), "Export cannot take promised usable stock");
    await Reject(async () => await warehouse.Operate(new() { Kind = "Count", Note = "Test", Items = [new() { IdIngredient = ingredient.Id, Quantity = 10, ExpectedQuantity = 30 }] }, "tester"), "Count cannot reduce below reservation");
    await warehouse.Operate(new() { Kind = "Export", Note = "Free stock only", Items = [new() { IdIngredient = ingredient.Id, Quantity = 10 }] }, "tester"); Clear();
    Check(await Held(ingredient.Id) == 20 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 20, "Export may use unreserved remainder");
    await flow.Cancel(secondLine.Id, new() { Count = 2 }); Clear();
    Check(await Held(ingredient.Id) == 0, "Cancelling all unsent items frees all stock");
    async Task<int?> Place(int tableId) { await using var other = Context(); try { return await new OrderFlow(other).Add(new() { IdTable = tableId, IdFood = food.Id, Count = 2 }); } catch (InvalidOperationException) { return null; } }
    var concurrent = await Task.WhenAll(Place(b.Id), Place(c.Id)); Clear();
    Check(concurrent.Count(id => id.HasValue) == 1 && await Held(ingredient.Id) == 20, "Two concurrent cashiers competing for last drinks have one successful reservation");
    var winner = concurrent.Single(id => id.HasValue)!.Value;
    async Task<bool> Pay() { await using var other = Context(); return await new BillController(other).Checkout(winner, new()) is OkObjectResult; }
    var payments = await Task.WhenAll(Pay(), Pay()); Clear();
    Check(payments.Count(ok => ok) == 1 && (await db.Ingredients.FindAsync(ingredient.Id))!.Quantity == 0 && await Held(ingredient.Id) == 0, "Concurrent automatic checkouts create one consumption and payment");
    Check(await db.CashEntries.CountAsync() == 2 && await db.StockLots.SumAsync(l => l.Quantity) == 0, "Concurrent checkout preserves stock lots and cash consistency");

    // Test expiry and frozen recipe on a separate ingredient to avoid changing completed orders.
    var cream = new Ingredient { Name = "Cream", Unit = "g", Quantity = 40, UnitCost = 1 };
    db.Ingredients.Add(cream); await db.SaveChangesAsync();
    db.StockLots.Add(new StockLot { IdIngredient = cream.Id, Code = "EXPIRED", Quantity = 30, UnitCost = 1, ExpiryDate = DateTime.Today.AddDays(-1) });
    var fresh = new StockLot { IdIngredient = cream.Id, Code = "FRESH", Quantity = 10, UnitCost = 1, ExpiryDate = DateTime.Today.AddDays(2) }; db.StockLots.Add(fresh); await db.SaveChangesAsync();
    var topping = new Food(); db.Foods.Add(topping);
    await new MenuCatalog(db).Save(topping, new CreateFoodDto { Name = "Cream drink", IdCategory = category.Id, Price = 10000, Recipe = [new() { IdIngredient = cream.Id, Amount = 5 }] }); Clear();
    await Reject(async () => await flow.Add(new() { IdTable = a.Id, IdFood = topping.Id, Count = 3 }), "Expired lots cannot be reserved");
    var expiryBill = await flow.Add(new() { IdTable = a.Id, IdFood = topping.Id, Count = 2 }); Clear();
    await warehouse.Operate(new() { Kind = "Disposal", Note = "Expired lot", Items = [new() { IdIngredient = cream.Id, Quantity = 30, IdLot = (await db.StockLots.SingleAsync(l => l.Code == "EXPIRED")).Id }] }, "tester"); Clear();
    Check(await Held(cream.Id) == 10 && (await db.Ingredients.FindAsync(cream.Id))!.Quantity == 10, "Expired disposal leaves usable reservations intact");
    var recipe = await db.Recipes.SingleAsync(r => r.IdFood == topping.Id); recipe.Amount = 500; await db.SaveChangesAsync(); Clear();
    Check(await Held(cream.Id) == 10, "Recipe edits cannot change existing reservations");
    fresh = (await db.StockLots.FindAsync(fresh.Id))!; fresh.ExpiryDate = DateTime.Today.AddDays(-1); await db.SaveChangesAsync(); Clear();
    Check(await checkout.Checkout(expiryBill, new()) is BadRequestObjectResult, "Expiry after reservation blocks automatic report and payment"); Clear();
    Check(await Held(cream.Id) == 10 && !await db.KitchenOrders.AnyAsync(o => o.IdBill == expiryBill), "Expired checkout keeps reservation and leaves no partial kitchen ticket");
    fresh = (await db.StockLots.FindAsync(fresh.Id))!; fresh.ExpiryDate = DateTime.Today.AddDays(2); await db.SaveChangesAsync(); Clear();
    Check(await checkout.Checkout(expiryBill, new()) is OkObjectResult, "Restored fresh stock uses frozen five-gram recipe on checkout"); Clear();
    Check((await db.Ingredients.FindAsync(cream.Id))!.Quantity == 0 && await Held(cream.Id) == 0, "Successful checkout clears reservation exactly");
    Console.WriteLine($"All {checks} reservation checks passed.");
}
finally { await db.Database.EnsureDeletedAsync(); }
