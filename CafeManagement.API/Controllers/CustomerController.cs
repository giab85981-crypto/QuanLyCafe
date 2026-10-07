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
public class CustomerController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(string? search = null)
    {
        var q = db.Customers.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); var phone = CustomerCatalog.Phone(term); q = q.Where(c => c.Name.Contains(term) || c.Code.Contains(term) || c.Phone.Contains(phone)); }
        return Ok(await q.OrderBy(c => c.Name).ThenBy(c => c.Id).Take(10).Select(c => new CustomerDto { Id = c.Id, Code = c.Code, Name = c.Name, Phone = c.Phone, Points = c.Points }).ToListAsync());
    }
    [HttpGet("manage")]
    public async Task<IActionResult> List(string? search = null, int? group = null, bool? active = null, string? gender = null, int? birthdayMonth = null, DateTime? from = null, DateTime? to = null, decimal? minSpend = null, decimal? maxSpend = null, string sort = "new", int page = 1, int pageSize = 15)
    {
        if (page < 1 || page > 1000000 || pageSize < 1 || pageSize > 100 || (birthdayMonth.HasValue && (birthdayMonth < 1 || birthdayMonth > 12)) || (from.HasValue && to.HasValue && from >= to) || minSpend < 0 || maxSpend < 0 || minSpend > maxSpend || !new[] { "new", "name", "spend" }.Contains(sort) || (gender != null && !new[] { "Nam", "Nữ", "Khác" }.Contains(gender))) return BadRequest("Bộ lọc không hợp lệ.");
        var q = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); var phone = CustomerCatalog.Phone(term); q = q.Where(c => c.Name.Contains(term) || c.Code.Contains(term) || c.Phone.Contains(phone) || c.Email.Contains(term)); }
        if (group.HasValue) q = group == 0 ? q.Where(c => c.IdGroup == null) : q.Where(c => c.IdGroup == group);
        if (active.HasValue) q = q.Where(c => c.IsActive == active);
        if (gender != null) q = q.Where(c => c.Gender == gender);
        if (birthdayMonth.HasValue) q = q.Where(c => c.Birthday.HasValue && c.Birthday.Value.Month == birthdayMonth);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from);
        if (to.HasValue) q = q.Where(c => c.CreatedAt < to);
        if (minSpend.HasValue) q = q.Where(c => (c.Bills.Where(b => b.Status == 1).Sum(b => (decimal?)b.TotalPrice) ?? 0) >= minSpend);
        if (maxSpend.HasValue) q = q.Where(c => (c.Bills.Where(b => b.Status == 1).Sum(b => (decimal?)b.TotalPrice) ?? 0) <= maxSpend);
        var total = await q.CountAsync(); var activeCount = await q.CountAsync(c => c.IsActive);
        var totalPoints = await q.SumAsync(c => (long?)c.Points) ?? 0;
        var spend = await db.Bills.Where(b => b.Status == 1 && b.IdCustomer != null && q.Select(c => c.Id).Contains(b.IdCustomer.Value)).SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
        var sorted = sort == "name" ? q.OrderBy(c => c.Name).ThenBy(c => c.Id) : sort == "spend" ? q.OrderByDescending(c => c.Bills.Where(b => b.Status == 1).Sum(b => (decimal?)b.TotalPrice) ?? 0).ThenByDescending(c => c.Id) : q.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);
        var rows = await sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(c => new {
            c.Id, c.Code, c.Name, c.Phone, c.Email, c.Gender, c.Birthday, c.Address, c.Note, c.IdGroup, GroupName = c.Group != null ? c.Group.Name : "Chưa phân nhóm", c.IsActive, c.CreatedAt, c.Points,
            Visits = c.Bills.Count(b => b.Status == 1), NetSpend = c.Bills.Where(b => b.Status == 1).Sum(b => (decimal?)b.TotalPrice) ?? 0,
            TotalSales = c.Bills.Where(b => b.Status == 1 || b.Status == 3).Sum(b => (decimal?)b.TotalPrice) ?? 0,
            RefundAmount = c.Bills.Where(b => b.Status == 3).Sum(b => (decimal?)b.RefundAmount) ?? 0,
            LastVisit = c.Bills.Where(b => b.Status == 1).Max(b => b.DateCheckOut),
            Estimated = c.Bills.Count(b => b.Status == 1 && !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100 && b.BillInfos.Any(i => i.Count > 0 && (i.UnitPrice ?? i.Food.Price) > 0))
        }).ToListAsync();
        return Ok(new { total, activeCount, totalPoints, spend, rows, page, pageSize });
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, int billPage = 1, int pointPage = 1)
    {
        if (billPage < 1 || pointPage < 1 || billPage > 1000000 || pointPage > 1000000) return BadRequest("Trang không hợp lệ.");
        var customer = await db.Customers.AsNoTracking().Include(c => c.Group).SingleOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound("Không tìm thấy khách hàng.");
        var bills = db.Bills.AsNoTracking().Where(b => b.IdCustomer == id);
        var billCount = await bills.CountAsync(); var visits = await bills.CountAsync(b => b.Status == 1);
        var netSpend = await bills.Where(b => b.Status == 1).SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
        var totalSales = await bills.Where(b => b.Status == 1 || b.Status == 3).SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
        var refundAmount = await bills.Where(b => b.Status == 3).SumAsync(b => (decimal?)b.RefundAmount) ?? 0;
        var history = await bills.OrderByDescending(b => b.DateCheckOut ?? b.DateCheckIn).ThenByDescending(b => b.Id).Skip((billPage - 1) * 20).Take(20).Select(b => new { b.Id, b.DateCheckIn, b.DateCheckOut, b.Status, b.OrderType, b.TotalPrice, b.RefundAmount, b.PaymentMethod, b.PointsEarned, b.PointsRedeemed, Estimated = !b.HasRecordedPayment && b.TotalPrice == 0 && b.Discount < 100 && b.BillInfos.Any(i => i.Count > 0 && (i.UnitPrice ?? i.Food.Price) > 0) }).ToListAsync();
        var entries = db.CustomerPointEntries.AsNoTracking().Where(e => e.IdCustomer == id);
        var pointCount = await entries.CountAsync();
        var points = await entries.OrderByDescending(e => e.Id).Skip((pointPage - 1) * 20).Take(20).Select(e => new { e.Id, e.IdBill, e.Kind, e.Delta, e.Balance, e.CreatedAt, e.CreatedBy }).ToListAsync();
        return Ok(new { customer = new { customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address, customer.Note, customer.Gender, customer.Birthday, customer.CreatedAt, customer.Points, customer.IdGroup, GroupName = customer.Group?.Name ?? "Chưa phân nhóm", customer.IsActive }, visits, netSpend, totalSales, refundAmount, billCount, pointCount, bills = history, points, billPage, pointPage });
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try { var c = await new CustomerCatalog(db).Save(dto); await tx.CommitAsync(); return Ok(CustomerCatalog.Dto(c)); }
        catch (InvalidOperationException e) { return BadRequest(e.Message); }
        catch (DbUpdateException) { return Conflict("Điện thoại đã tồn tại hoặc dữ liệu vừa được cập nhật. Vui lòng tải lại."); }
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateCustomerDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var c = await db.Customers.FindAsync(id); if (c == null) return NotFound();
        try { await new CustomerCatalog(db).Save(dto, c); await tx.CommitAsync(); return Ok(CustomerCatalog.Dto(c)); }
        catch (InvalidOperationException e) { return BadRequest(e.Message); }
        catch (DbUpdateException) { return Conflict("Điện thoại đã tồn tại. Vui lòng kiểm tra lại."); }
    }
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> Status(int id, CustomerStateWrite dto)
    {
        var c = await db.Customers.FindAsync(id); if (c == null) return NotFound(); c.IsActive = dto.IsActive; await db.SaveChangesAsync(); return Ok();
    }
    [HttpGet("groups")]
    public async Task<IActionResult> Groups() => Ok(await db.CustomerGroups.AsNoTracking().OrderBy(g => g.Name).ToListAsync());
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup(CustomerGroupWrite dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100) return BadRequest("Nhập tên nhóm tối đa 100 ký tự.");
        var name = dto.Name.Trim(); if (await db.CustomerGroups.AnyAsync(g => g.Name == name)) return BadRequest("Tên nhóm đã tồn tại.");
        var group = new CustomerGroup { Name = name }; db.Add(group);
        try { await db.SaveChangesAsync(); return Ok(group); } catch (DbUpdateException) { return Conflict("Tên nhóm đã tồn tại."); }
    }
    [HttpPut("groups/{id:int}")]
    public async Task<IActionResult> UpdateGroup(int id, CustomerGroupWrite dto)
    {
        var g = await db.CustomerGroups.FindAsync(id); if (g == null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100) return BadRequest("Nhập tên nhóm tối đa 100 ký tự.");
        var name = dto.Name.Trim(); if (await db.CustomerGroups.AnyAsync(x => x.Id != id && x.Name == name)) return BadRequest("Tên nhóm đã tồn tại.");
        g.Name = name; try { await db.SaveChangesAsync(); return Ok(); } catch (DbUpdateException) { return Conflict("Tên nhóm đã tồn tại."); }
    }
    [HttpDelete("groups/{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var g = await db.CustomerGroups.FindAsync(id); if (g == null) return NotFound();
        if (await db.Customers.AnyAsync(c => c.IdGroup == id)) return BadRequest("Nhóm đang có khách hàng. Chuyển khách sang nhóm khác trước.");
        db.Remove(g); try { await db.SaveChangesAsync(); return Ok(); } catch (DbUpdateException) { return Conflict("Nhóm đang được sử dụng."); }
    }
    [HttpPost("import")]
    public async Task<IActionResult> Import(CustomerImportWrite dto)
    {
        if (dto.Items == null || dto.Items.Count == 0 || dto.Items.Count > 500) return BadRequest("Mỗi lần nhập từ 1–500 khách hàng.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var errors = new List<object>(); var seen = new HashSet<string>(); var catalog = new CustomerCatalog(db);
        for (var i = 0; i < dto.Items.Count; i++) {
            try { if (dto.Items[i] == null) throw new InvalidOperationException("Dòng dữ liệu trống."); await catalog.Validate(dto.Items[i]); if (!seen.Add(CustomerCatalog.Phone(dto.Items[i].Phone))) throw new InvalidOperationException("Điện thoại trùng trong file."); }
            catch (InvalidOperationException e) { errors.Add(new { row = i + 2, message = e.Message }); }
        }
        if (dto.Preview || errors.Count > 0) return Ok(new { valid = errors.Count == 0, count = dto.Items.Count, errors });
        try { foreach (var item in dto.Items) await catalog.Save(item); await tx.CommitAsync(); return Ok(new { valid = true, count = dto.Items.Count, errors }); }
        catch (DbUpdateException) { return Conflict("Dữ liệu trùng với thay đổi mới. Kiểm tra lại file trước khi nhập."); }
    }
}
