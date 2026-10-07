using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CafeManagement.API.Controllers;
[ApiController, Route("api/[controller]"), Authorize]
public class AccountController(AppDbContext db) : ControllerBase
{
    string? Actor => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public static string? PasswordError(string? password) => password == null || password.Length < 8 || System.Text.Encoding.UTF8.GetByteCount(password) > 72 ? "Mật khẩu cần ít nhất 8 ký tự, tối đa 72 byte." : null;
    public static async Task<string?> Guard(AppDbContext db, Account account, string? actor, bool losingAdmin)
    {
        if (string.Equals(account.UserName, actor, StringComparison.OrdinalIgnoreCase) && losingAdmin) return "Không thể khóa hoặc hạ quyền tài khoản đang đăng nhập.";
        if (losingAdmin && account.IsActive && await db.Roles.AnyAsync(r => r.Id == account.IdRole && r.Name == "Admin") && !await db.Accounts.AnyAsync(a => a.IsActive && a.UserName != account.UserName && a.Role!.Name == "Admin")) return "Phải giữ ít nhất một quản trị viên đang hoạt động.";
        return null;
    }
    [NonAction]
    public async Task<bool> MayManage(Account? target, int roleId)
    {
        if (HttpContext?.User.Identity?.IsAuthenticated != true || User.IsInRole("Admin")) return true; // Controller-only domain checks have no HTTP identity.
        var role = await db.Roles.FindAsync(roleId);
        if (role?.Name == "Admin" || target != null && await db.Roles.AnyAsync(r => r.Id == target.IdRole && r.Name == "Admin")) return false;
        var mine = await CafeManagement.API.Services.DynamicAccess.Effective(db, Actor ?? "");
        var granted = await db.RolePermissions.Where(p => p.IdRole == roleId).Select(p => p.Permission.Code).ToListAsync();
        if (target != null) granted.AddRange(await CafeManagement.API.Services.DynamicAccess.Effective(db, target.UserName));
        return granted.All(mine.Contains);
    }
    [HttpGet("roles")] public async Task<IActionResult> Roles() => Ok(await db.Roles.OrderBy(r => r.Id).Select(r => new { r.Id, r.Name, r.Description }).ToListAsync());
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await db.Accounts.OrderBy(a => a.UserName).Select(a => new { a.UserName, a.DisplayName, a.IdRole, RoleName = a.Role!.Name, a.IsActive, Linked = db.Employees.Any(e => e.UserName == a.UserName) }).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(CreateAccountDto dto)
    {
        dto.UserName = (dto.UserName ?? "").Trim(); dto.DisplayName = (dto.DisplayName ?? "").Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.UserName, @"^[a-zA-Z0-9_.-]{3,100}$") || dto.DisplayName.Length is < 1 or > 100) return BadRequest(new { message = "Tên đăng nhập 3–100 ký tự chữ, số, dấu chấm, gạch; nhập tên hiển thị." });
        var error = PasswordError(dto.PassWord); if (error != null) return BadRequest(new { message = error });
        if (await db.Accounts.AnyAsync(a => a.UserName == dto.UserName)) return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });
        if (!await db.Roles.AnyAsync(r => r.Id == dto.IdRole)) return BadRequest(new { message = "Vai trò không hợp lệ." });
        if (!await MayManage(null, dto.IdRole)) return Forbid();
        db.Accounts.Add(new() { UserName = dto.UserName, DisplayName = dto.DisplayName, PassWord = BCrypt.Net.BCrypt.HashPassword(dto.PassWord), IdRole = dto.IdRole });
        await db.SaveChangesAsync(); return Ok(new { userName = dto.UserName });
    }
    [HttpPut("{username}")] public async Task<IActionResult> Update(string username, UpdateAccountDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.Accounts.FindAsync(username); if (account == null) return NotFound();
        var role = await db.Roles.FindAsync(dto.IdRole); if (role == null) return BadRequest(new { message = "Vai trò không hợp lệ." });
        if (!await MayManage(account, dto.IdRole)) return Forbid();
        var error = await Guard(db, account, Actor, role.Name != "Admin" && account.IdRole != dto.IdRole); if (error != null) return BadRequest(new { message = error });
        if (string.IsNullOrWhiteSpace(dto.DisplayName) || dto.DisplayName.Length > 100) return BadRequest(new { message = "Tên hiển thị 1–100 ký tự." });
        if (!string.IsNullOrEmpty(dto.PassWord)) { error = PasswordError(dto.PassWord); if (error != null) return BadRequest(new { message = error }); account.PassWord = BCrypt.Net.BCrypt.HashPassword(dto.PassWord); account.SecurityVersion++; }

        account.IdRole = dto.IdRole; account.DisplayName = dto.DisplayName.Trim();
        await Audit(username, "Cập nhật vai trò / tài khoản"); await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
    [HttpPut("{username}/status")] public async Task<IActionResult> UpdateStatus(string username, UpdateAccountStatusDto dto)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.Accounts.FindAsync(username); if (account == null) return NotFound();
        if (!await MayManage(account, account.IdRole)) return Forbid();
        var error = await Guard(db, account, Actor, !dto.IsActive); if (error != null) return BadRequest(new { message = error });
        if (dto.IsActive && await db.Employees.AnyAsync(e => e.UserName == username && !e.IsActive)) return BadRequest(new { message = "Nhân viên đã nghỉ việc. Hãy chuyển sang đi làm lại trước." });
        if (account.IsActive != dto.IsActive) account.SecurityVersion++;
        account.IsActive = dto.IsActive; await Audit(username, dto.IsActive ? "Mở khóa đăng nhập" : "Khóa đăng nhập");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok();
    }
    [HttpDelete("{username}")] public IActionResult Delete(string username) => BadRequest(new { message = "Hãy khóa tài khoản hoặc cho nhân viên nghỉ việc để giữ lịch sử giao dịch." });
    async Task Audit(string username, string action) { var id = await db.Employees.Where(e => e.UserName == username).Select(e => (int?)e.Id).SingleOrDefaultAsync(); if (id.HasValue) db.EmployeeActivities.Add(new() { EmployeeId = id.Value, Actor = Actor ?? "admin", Action = action }); }
}
