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
public class TableFoodController(AppDbContext db) : ControllerBase
{
    async Task<IActionResult> Work(Func<Task<object>> action) { try { return Ok(await action()); } catch (InvalidOperationException e) { return BadRequest(e.Message); } }
    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas() => Ok(await db.Areas.OrderBy(a => a.Name).Select(a => new { a.Id, a.Name, a.IsActive, TableCount = a.TableFoods.Count }).ToListAsync());
    async Task<object> SaveArea(int? id, AreaWrite dto)
    {
        WarehouseFlow.Text(dto.Name, 100, "Tên khu vực", true);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Areas.AnyAsync(a => a.Id != id && a.Name == dto.Name.Trim())) throw new InvalidOperationException("Tên khu vực đã tồn tại.");
        if (id.HasValue && !dto.IsActive && await db.Bills.AnyAsync(b => b.Status == 0 && b.TableFood != null && b.TableFood.IdArea == id)) throw new InvalidOperationException("Khu vực đang có bàn phục vụ, chưa thể ngừng hoạt động.");
        var area = id.HasValue ? await db.Areas.FindAsync(id) : new Area(); if (area == null) throw new InvalidOperationException("Không tìm thấy khu vực.");
        if (!id.HasValue) db.Areas.Add(area); area.Name = dto.Name.Trim(); area.IsActive = dto.IsActive;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { area.Id };
    }
    [HttpPost("areas")] public Task<IActionResult> CreateArea(AreaWrite dto) => Work(() => SaveArea(null, dto));
    [HttpPut("areas/{id}")] public Task<IActionResult> EditArea(int id, AreaWrite dto) => Work(() => SaveArea(id, dto));
    [HttpDelete("areas/{id}")] public Task<IActionResult> DeleteArea(int id) => Work(async () => { if (await db.TableFoods.AnyAsync(t => t.IdArea == id)) throw new InvalidOperationException("Chuyển bàn sang khu vực khác trước khi xóa khu vực trống."); var area = await db.Areas.FindAsync(id) ?? throw new InvalidOperationException("Không tìm thấy khu vực."); db.Areas.Remove(area); await db.SaveChangesAsync(); return new { message = "Đã xóa khu vực trống." }; });
    [HttpGet]
    public async Task<IActionResult> GetAll(int? areaId, string? search, string? status)
    {
        var rows = await db.TableFoods.Include(t => t.Area).OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToListAsync();
        var bills = await db.Bills.Include(b => b.BillInfos).ThenInclude(i => i.Food).Where(b => b.Status == 0).ToListAsync();
        var result = rows.Select(t => { var bill = bills.FirstOrDefault(b => b.IdTable == t.Id); var active = t.IsActive && t.Area?.IsActive != false; return new { t.Id, t.Name, t.Seats, t.Note, t.SortOrder, t.IsActive, t.IdArea, AreaName = t.Area?.Name ?? "Chưa xếp", AreaActive = t.Area?.IsActive ?? true, Status = !active ? "Ngừng hoạt động" : bill != null ? "Có người" : "Trống", ActiveBillId = bill?.Id ?? 0, GuestCount = bill?.GuestCount, Amount = bill?.BillInfos.Sum(i => i.Count * (i.UnitPrice ?? i.Food.Price)) ?? 0 }; });
        if (areaId.HasValue) result = result.Where(t => t.IdArea == areaId);
        if (!string.IsNullOrWhiteSpace(search)) result = result.Where(t => t.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả") result = result.Where(t => t.Status == status);
        return Ok(result);
    }
    [NonAction]
    public async Task<string?> Validate(TableWrite dto, int? id = null)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100 || dto.Seats < 1 || dto.Seats > 1000 || dto.SortOrder < 0 || dto.SortOrder > 1000000 || dto.Note?.Length > 255) return "Tên, số ghế (1–1.000), thứ tự hoặc ghi chú không hợp lệ.";
        if (await db.TableFoods.AnyAsync(t => t.Id != id && t.Name == dto.Name.Trim())) return "Tên phòng/bàn đã tồn tại.";
        if (dto.IdArea.HasValue && !await db.Areas.AnyAsync(a => a.Id == dto.IdArea)) return "Khu vực không tồn tại.";
        return null;
    }
    async Task<object> Save(int? id, TableWrite dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var error = await Validate(dto, id); if (error != null) throw new InvalidOperationException(error);
        bool occupied = id.HasValue && await db.Bills.AnyAsync(b => b.IdTable == id && b.Status == 0);
        bool areaStopped = dto.IdArea.HasValue && !await db.Areas.AnyAsync(a => a.Id == dto.IdArea && a.IsActive);
        if (occupied && (!dto.IsActive || areaStopped)) throw new InvalidOperationException("Bàn đang phục vụ: chuyển/gộp hoặc thanh toán trước khi ngừng hoạt động.");
        var table = id.HasValue ? await db.TableFoods.FindAsync(id) : new TableFood(); if (table == null) throw new InvalidOperationException("Không tìm thấy bàn.");
        if (!id.HasValue) db.TableFoods.Add(table); table.Name = dto.Name.Trim(); table.Seats = dto.Seats; table.Note = dto.Note?.Trim(); table.SortOrder = dto.SortOrder; table.IdArea = dto.IdArea; table.IsActive = dto.IsActive; table.Status = occupied ? "Có người" : "Trống";
        await db.SaveChangesAsync(); await tx.CommitAsync(); return new { table.Id };
    }
    [HttpPost] public Task<IActionResult> Create(TableWrite dto) => Work(() => Save(null, dto));
    [HttpPut("{id}")] public Task<IActionResult> Edit(int id, TableWrite dto) => Work(() => Save(id, dto));
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id) => Work(async () => {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Bills.AnyAsync(b => b.IdTable == id && b.Status == 0)) throw new InvalidOperationException("Bàn đang có hóa đơn, chưa thể ngừng hoạt động.");
        var table = await db.TableFoods.FindAsync(id) ?? throw new InvalidOperationException("Không tìm thấy bàn."); table.IsActive = false; await db.SaveChangesAsync(); await tx.CommitAsync(); return new { message = "Đã ngừng hoạt động, giữ nguyên lịch sử." };
    });
    [HttpPut("{id}/status")]
    public Task<IActionResult> UpdateStatus(int id, UpdateTableStatusDto dto) => Work(async () => {
        if (dto.Status != "Trống" && dto.Status != "Có người") throw new InvalidOperationException("Trạng thái phục vụ được cập nhật theo hóa đơn.");
        var table = await db.TableFoods.FindAsync(id) ?? throw new InvalidOperationException("Không tìm thấy bàn."); var occupied = await db.Bills.AnyAsync(b => b.IdTable == id && b.Status == 0);
        if ((dto.Status == "Có người") != occupied) throw new InvalidOperationException("Không thể thay đổi trạng thái trái với hóa đơn hiện tại."); table.Status = dto.Status; await db.SaveChangesAsync(); return new { table.Id };
    });
    [HttpPost("transfer")] public Task<IActionResult> Transfer(TableTransferWrite dto) => Work(async () => new { idBill = await new TableFlow(db).Transfer(dto, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system") });
    [HttpGet("operations")] public async Task<IActionResult> Operations() => Ok(await db.TableOperations.OrderByDescending(o => o.Id).Take(200).ToListAsync());
    [HttpPost("excel")]
    public Task<IActionResult> Excel(TableExcelWrite dto) => Work(async () => {
        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.Count > 500) throw new InvalidOperationException("File cần từ 1 đến 500 bàn.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var errors = new List<object>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int n = 0; n < dto.Items.Count; n++) { var row = dto.Items[n]; var error = await Validate(row); if (row != null && !names.Add(row.Name?.Trim() ?? "")) error = "Tên lặp trong file."; if (error != null) errors.Add(new { row = n + 2, message = error }); }
        if (!dto.Preview && errors.Count == 0) { foreach (var row in dto.Items) db.TableFoods.Add(new TableFood { Name = row.Name.Trim(), Seats = row.Seats, SortOrder = row.SortOrder, Note = row.Note, IdArea = row.IdArea, IsActive = row.IsActive }); await db.SaveChangesAsync(); await tx.CommitAsync(); }
        return new { valid = errors.Count == 0, count = dto.Items.Count, errors };
    });
}
