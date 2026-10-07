using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
namespace CafeManagement.API.Controllers;
[ApiController, Route("api/[controller]"), Authorize]
public class EmployeeController(AppDbContext db) : ControllerBase
{
    object Dto(Employee e) => new { e.Id, Code = $"NV{e.Id:D6}", e.Name, e.Phone, e.Email, e.Address, e.Gender, e.Birthday, e.HireDate, e.Department, e.Position, e.Note, e.IsActive, e.UserName,
        AccountActive = e.Account?.IsActive, IdRole = e.Account?.IdRole, RoleName = e.Account?.Role?.Name };
    IQueryable<Employee> Query => db.Employees.Include(e => e.Account).ThenInclude(a => a!.Role);
    [HttpGet] public async Task<IActionResult> List(string? search = null, bool? active = null, string? department = null, string? position = null)
    {
        var q = Query.AsNoTracking();
        if (active.HasValue) q = q.Where(e => e.IsActive == active);
        if (!string.IsNullOrWhiteSpace(department)) q = q.Where(e => e.Department == department);
        if (!string.IsNullOrWhiteSpace(position)) q = q.Where(e => e.Position == position);
        var rows = await q.OrderBy(e => e.Name).ToListAsync();
        if (!string.IsNullOrWhiteSpace(search)) rows = rows.Where(e => $"NV{e.Id:D6} {e.Name} {e.Phone} {e.UserName}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(new { items = rows.Select(Dto), total = rows.Count,
            working = await db.Employees.CountAsync(e => e.IsActive), retired = await db.Employees.CountAsync(e => !e.IsActive),
            departments = await db.Employees.Where(e => e.Department != "").Select(e => e.Department).Distinct().OrderBy(x => x).ToListAsync(),
            positions = await db.Employees.Where(e => e.Position != "").Select(e => e.Position).Distinct().OrderBy(x => x).ToListAsync() });
    }
    [HttpGet("{id:int}/activities")] public async Task<IActionResult> Activities(int id) => Ok(await db.EmployeeActivities.Where(e => e.EmployeeId == id).OrderByDescending(e => e.Id).Take(50).ToListAsync());
    [HttpPost] public Task<IActionResult> Create(EmployeeWrite dto) => Write(null, dto);
    [HttpPut("{id:int}")] public Task<IActionResult> Update(int id, EmployeeWrite dto) => Write(id, dto);
    async Task<IActionResult> Write(int? id, EmployeeWrite dto)
    {
        dto.Name = (dto.Name ?? "").Trim(); dto.Phone = (dto.Phone ?? "").Trim(); dto.Email = (dto.Email ?? "").Trim();
        if (dto.Name.Length == 0) return BadRequest(new { message = "Nhập họ tên nhân viên." });
        if (dto.Phone != "" && !System.Text.RegularExpressions.Regex.IsMatch(dto.Phone, @"^\+?[0-9 ()-]{8,20}$")) return BadRequest(new { message = "Số điện thoại không hợp lệ." });
        if (dto.Email != "" && !new EmailAddressAttribute().IsValid(dto.Email)) return BadRequest(new { message = "Email không hợp lệ." });
        if (dto.Birthday > DateTime.Today || (dto.HireDate.HasValue && dto.Birthday >= dto.HireDate)) return BadRequest(new { message = "Kiểm tra ngày sinh và ngày vào làm." });
        if (!new[] { "", "Nam", "Nữ", "Khác" }.Contains(dto.Gender)) return BadRequest(new { message = "Giới tính không hợp lệ." });
        var employee = id.HasValue ? await Query.SingleOrDefaultAsync(e => e.Id == id) : new Employee();
        if (employee == null) return NotFound();
        var username = string.IsNullOrWhiteSpace(dto.UserName) ? null : dto.UserName.Trim();
        // A linked login remains attached to preserve employment and transaction history.
        if (id.HasValue && employee.UserName != null && employee.UserName != username) return BadRequest(new { message = "Không thể đổi hoặc gỡ tài khoản đã gắn. Hãy khóa tài khoản nếu không còn sử dụng." });
        if (username != null && (!await db.Accounts.AnyAsync(a => a.UserName == username) || await db.Employees.AnyAsync(e => e.UserName == username && e.Id != employee.Id))) return BadRequest(new { message = "Tài khoản không tồn tại hoặc đã thuộc nhân viên khác." });
        if (!employee.IsActive && username != null && employee.UserName == null) return BadRequest(new { message = "Cho nhân viên đi làm lại trước khi gắn tài khoản." });
        if (HttpContext?.User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin") && username != employee.UserName && username != null) return Forbid();
        employee.Name = dto.Name; employee.Phone = dto.Phone; employee.Email = dto.Email; employee.Address = (dto.Address ?? "").Trim(); employee.Gender = dto.Gender;
        employee.Birthday = dto.Birthday?.Date; employee.HireDate = dto.HireDate?.Date; employee.Department = (dto.Department ?? "").Trim(); employee.Position = (dto.Position ?? "").Trim(); employee.Note = (dto.Note ?? "").Trim(); employee.UserName = username;
        if (!id.HasValue) db.Employees.Add(employee);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        db.EmployeeActivities.Add(new() { EmployeeId = employee.Id, Actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin", Action = id.HasValue ? "Cập nhật hồ sơ" : "Thêm nhân viên" });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        employee = await Query.SingleAsync(e => e.Id == employee.Id);
        return Ok(Dto(employee));
    }
    [HttpPost("{id:int}/login")] public async Task<IActionResult> Login(int id, CreateAccountDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var employee = await db.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        if (!employee.IsActive || employee.UserName != null) return BadRequest(new { message = "Nhân viên đã nghỉ hoặc đã có tài khoản." });
        dto.DisplayName = employee.Name;
        var result = await new AccountController(db) { ControllerContext = ControllerContext }.Create(dto);
        if (result is not OkObjectResult) return result;
        employee.UserName = dto.UserName.Trim();
        db.EmployeeActivities.Add(new() { EmployeeId = id, Actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin", Action = "Cấp tài khoản đăng nhập" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
    [HttpPut("{id:int}/status")] public async Task<IActionResult> Status(int id, UpdateAccountStatusDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var employee = await Query.SingleOrDefaultAsync(e => e.Id == id);
        if (employee == null) return NotFound();
        if (employee.Account != null && !await new AccountController(db) { ControllerContext = ControllerContext }.MayManage(employee.Account, employee.Account.IdRole)) return Forbid();
        if (!dto.IsActive && employee.Account != null) {
            var error = await AccountController.Guard(db, employee.Account, User.FindFirstValue(ClaimTypes.NameIdentifier), true);
            if (error != null) return BadRequest(new { message = error });
            employee.Account.IsActive = false; employee.Account.SecurityVersion++;
        }
        employee.IsActive = dto.IsActive;
        db.EmployeeActivities.Add(new() { EmployeeId = id, Actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin", Action = dto.IsActive ? "Đi làm lại (tài khoản cần mở khóa riêng)" : "Nghỉ việc; khóa tài khoản đăng nhập" });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
}
