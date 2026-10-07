using System.Security.Claims;
using System.Data;
using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeManagement.API.Controllers;
[Authorize, ApiController, Route("api/[controller]")]
public class WarehouseController(AppDbContext db) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    private async Task<IActionResult> Action(Func<Task<object>> work)
    { try { return Ok(await work()); } catch (InvalidOperationException ex) { return BadRequest(ex.Message); } }
    [HttpGet("ingredients")]
    public async Task<IActionResult> Ingredients()
    {
        var rows = await db.Ingredients.Include(i => i.Group).Include(i => i.Units).Include(i => i.Lots).AsSplitQuery().OrderBy(i => i.Id).ToListAsync();
        var held = await new StockReservations(db).Held();
        return Ok(rows.Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.IdGroup, GroupName = i.Group?.Name ?? "Chưa phân nhóm", i.IsActive, i.Quantity, i.MinQuantity, i.UnitCost,
            ReservedQuantity = held.GetValueOrDefault(i.Id),
            AvailableQuantity = Math.Max(0, StockReservations.Usable(i) - held.GetValueOrDefault(i.Id)),
            StockValue = i.Lots.Any() ? i.Lots.Sum(l => (decimal)l.Quantity * l.UnitCost) : (decimal)i.Quantity * i.UnitCost,
            ExpiredQuantity = i.Lots.Where(l => !StockLots.Usable(l)).Sum(l => l.Quantity),
            Units = i.Units.OrderBy(u => u.Id).Select(u => new { u.Id, u.Name, u.Factor }),
            Lots = i.Lots.Where(l => l.Quantity > 0).OrderBy(l => l.ExpiryDate ?? DateTime.MaxValue).Select(l => new { l.Id, l.Code, l.Quantity, l.UnitCost, l.ExpiryDate, l.CreatedAt, l.IdImportReceipt }) }));
    }
    [HttpPost("ingredients")] public async Task<IActionResult> CreateIngredient(WarehouseIngredientWrite dto) => await Action(async () => { await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var row = await new WarehouseCatalog(db).Save(dto, null, Actor); await tx.CommitAsync(); return new { row.Id }; });
    [HttpPut("ingredients/{id}")] public async Task<IActionResult> EditIngredient(int id, WarehouseIngredientWrite dto) => await Action(async () => { await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var row = await new WarehouseCatalog(db).Save(dto, id, Actor); await tx.CommitAsync(); return new { row.Id }; });
    [HttpDelete("ingredients/{id}")]
    public async Task<IActionResult> StopIngredient(int id)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await new StockReservations(db).Lock();
        var row = await db.Ingredients.FindAsync(id); if (row == null) return NotFound();
        if ((await new StockReservations(db).Held()).GetValueOrDefault(id) > 0) return BadRequest("Nguyên liệu đang giữ cho đơn chưa báo bếp. Hãy xử lý các đơn trước.");
        row.IsActive = false; await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Đã ngừng sử dụng, lịch sử và tồn kho được giữ nguyên." });
    }
    [HttpGet("groups")] public async Task<IActionResult> Groups() => Ok(await db.IngredientGroups.OrderBy(g => g.Name).ToListAsync());
    [HttpPost("groups")] public async Task<IActionResult> AddGroup(CreateFoodCategoryDto dto) => await Action(async () => { WarehouseFlow.Text(dto.Name, 100, "Tên nhóm", true); if (await db.IngredientGroups.AnyAsync(g => g.Name == dto.Name.Trim())) throw new InvalidOperationException("Tên nhóm đã tồn tại."); var row = new IngredientGroup { Name = dto.Name.Trim() }; db.IngredientGroups.Add(row); await db.SaveChangesAsync(); return new { row.Id }; });
    [HttpPut("groups/{id}")] public async Task<IActionResult> EditGroup(int id, CreateFoodCategoryDto dto) => await Action(async () => { WarehouseFlow.Text(dto.Name, 100, "Tên nhóm", true); if (await db.IngredientGroups.AnyAsync(g => g.Id != id && g.Name == dto.Name.Trim())) throw new InvalidOperationException("Tên nhóm đã tồn tại."); var row = await db.IngredientGroups.FindAsync(id) ?? throw new InvalidOperationException("Không tìm thấy nhóm."); row.Name = dto.Name.Trim(); await db.SaveChangesAsync(); return new { row.Id }; });
    [HttpDelete("groups/{id}")] public async Task<IActionResult> DeleteGroup(int id) => await Action(async () => { if (await db.Ingredients.AnyAsync(i => i.IdGroup == id)) throw new InvalidOperationException("Nhóm đang có nguyên liệu, hãy chuyển nhóm trước."); var row = await db.IngredientGroups.FindAsync(id) ?? throw new InvalidOperationException("Nhóm không tồn tại."); db.IngredientGroups.Remove(row); await db.SaveChangesAsync(); return new { message = "Đã xóa nhóm trống." }; });
    [HttpGet("suppliers")]
    public async Task<IActionResult> Suppliers()
    {
        var suppliers = await db.Suppliers.OrderBy(s => s.Id).ToListAsync();
        var receipts = await db.ImportReceipts.ToListAsync();
        var paid = await db.CashEntries.Where(c => c.IdImportReceipt != null).GroupBy(c => c.IdImportReceipt!.Value).Select(g => new { Id = g.Key, Paid = g.Sum(c => c.Direction == "Out" ? c.Amount : -c.Amount) }).ToDictionaryAsync(g => g.Id, g => g.Paid);
        return Ok(suppliers.Select(s => new { s.Id, s.Name, s.Phone, s.Address, s.IsActive, TotalPurchases = receipts.Where(r => r.IdSupplier == s.Id).Sum(r => r.TotalAmount),
            Debt = receipts.Where(r => r.IdSupplier == s.Id && r.HasPaymentTracking).Sum(r => r.TotalAmount - paid.GetValueOrDefault(r.Id)), UnknownPayments = receipts.Count(r => r.IdSupplier == s.Id && !r.HasPaymentTracking) }));
    }    private async Task<object> SaveSupplier(int? id, SupplierWrite dto)
    {
        WarehouseFlow.Text(dto.Name, 150, "Tên nhà cung cấp", true); WarehouseFlow.Text(dto.Phone, 20, "Điện thoại"); WarehouseFlow.Text(dto.Address, 500, "Địa chỉ");
        if (await db.Suppliers.AnyAsync(s => s.Id != id && s.Name == dto.Name.Trim())) throw new InvalidOperationException("Nhà cung cấp đã tồn tại.");
        var row = id.HasValue ? await db.Suppliers.FindAsync(id) : new Supplier(); if (row == null) throw new InvalidOperationException("Nhà cung cấp không tồn tại."); if (!id.HasValue) db.Suppliers.Add(row);
        row.Name = dto.Name.Trim(); row.Phone = dto.Phone.Trim(); row.Address = dto.Address.Trim(); row.IsActive = dto.IsActive; await db.SaveChangesAsync(); return new { row.Id };
    }
    [HttpPost("suppliers")] public async Task<IActionResult> AddSupplier(SupplierWrite dto) => await Action(() => SaveSupplier(null, dto));
    [HttpPut("suppliers/{id}")] public async Task<IActionResult> EditSupplier(int id, SupplierWrite dto) => await Action(() => SaveSupplier(id, dto));
    [HttpGet("receipts")]
    public async Task<IActionResult> Receipts() => Ok(await db.ImportReceipts.AsSplitQuery().OrderByDescending(r => r.Id).Select(r => new { r.Id, Code = "PN" + r.Id, r.ImportDate, SupplierName = r.Supplier.Name, r.IdSupplier, r.UserName, r.Note, r.TotalAmount, r.HasPaymentTracking,
        PaidAmount = r.HasPaymentTracking ? (decimal?)db.CashEntries.Where(c => c.IdImportReceipt == r.Id).Sum(c => c.Direction == "Out" ? c.Amount : -c.Amount) : null,
        Items = r.ImportDetails.Select(d => new { d.IdIngredient, IngredientName = d.Ingredient.Name, d.Count, BaseUnit = d.Ingredient.Unit, d.InputQuantity, d.UnitName, d.ConversionFactor, d.InputUnitPrice, d.IdLot, LotCode = d.Lot != null ? d.Lot.Code : "", ExpiryDate = d.Lot != null ? d.Lot.ExpiryDate : null }),
        Payments = db.CashEntries.Where(c => c.IdImportReceipt == r.Id).OrderBy(c => c.Id).Select(c => new { c.Id, c.Amount, c.PaymentMethod, c.CreatedAt, c.Note, c.CreatedBy }).ToList() }).ToListAsync());
    [HttpPost("receipts")] public async Task<IActionResult> Import(WarehouseImportWrite dto) => await Action(async () => new { id = await new WarehouseFlow(db).Import(dto, Actor) });
    [HttpPost("receipts/{id}/payments")] public async Task<IActionResult> Pay(int id, SupplierPaymentWrite dto) => await Action(async () => new { id = await new WarehouseFlow(db).Pay(id, dto, Actor) });
    [HttpGet("documents")]
    public async Task<IActionResult> Documents() => Ok(await db.WarehouseDocuments.OrderByDescending(d => d.Id).Select(d => new { d.Id, d.Kind, d.CreatedBy, d.CreatedAt, d.Note,
        Lines = d.Lines.Select(l => new { l.IdIngredient, IngredientName = l.Ingredient.Name, Unit = l.Ingredient.Unit, l.BeforeQuantity, l.AfterQuantity, l.UnitCost }) }).ToListAsync());
    [HttpPost("documents")] public async Task<IActionResult> Document(WarehouseOperationWrite dto)
    {
        var permission = dto.Kind switch { "Export" => "INVENTORY_EXPORT", "Count" => "INVENTORY_COUNT", "Disposal" => "INVENTORY_DISPOSAL", _ => "INVALID" };
        if (HttpContext?.User.Identity?.IsAuthenticated == true && !DynamicAccess.Has(User, permission)) return Forbid();
        return await Action(async () => new { id = await new WarehouseFlow(db).Operate(dto, Actor) });
    }
    [HttpGet("movements")]
    public async Task<IActionResult> Movements(DateTime? from, DateTime? to, int? ingredientId)
    {
        var query = db.StockMovements.AsQueryable();
        if (from.HasValue) query = query.Where(m => m.CreatedAt >= from.Value.Date); if (to.HasValue) { var end = to.Value.Date.AddDays(1); query = query.Where(m => m.CreatedAt < end); }
        if (ingredientId.HasValue) query = query.Where(m => m.IdIngredient == ingredientId);
        return Ok(await query.OrderByDescending(m => m.Id).Take(1000).Select(m => new { m.Id, m.IdIngredient, IngredientName = m.Ingredient.Name, m.Ingredient.Unit, m.Quantity, m.UnitCost, m.Kind, m.Note, m.CreatedAt, m.CreatedBy, m.IdDocument, m.IdImportReceipt, LotCode = m.Lot != null ? m.Lot.Code : "" }).ToListAsync());
    }
    [HttpPost("excel")]
    public async Task<IActionResult> Excel(WarehouseExcelWrite dto)
    {
        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.Count > 500) return BadRequest("File cần từ 1 đến 500 nguyên liệu.");
        var errors = new List<object>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < dto.Items.Count; index++) { var row = dto.Items[index]; var error = await new WarehouseCatalog(db).Validate(row);
            if (row != null && !names.Add(row.Name?.Trim() ?? "")) error = "Tên lặp trong file."; if (row != null && !string.IsNullOrWhiteSpace(row.Code) && !codes.Add(row.Code.Trim())) error = "Mã lặp trong file.";
            if (error != null) errors.Add(new { row = index + 2, message = error }); }
        if (dto.Preview || errors.Count > 0) return Ok(new { valid = errors.Count == 0, count = dto.Items.Count, errors });
        return await Action(async () => { await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); foreach (var row in dto.Items) await new WarehouseCatalog(db).Save(row, null, Actor); await tx.CommitAsync(); return new { valid = true, count = dto.Items.Count, errors }; });
    }
}



