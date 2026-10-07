using CafeManagement.API.Data;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace CafeManagement.API.Controllers;
public class RoleAccessWrite { public string Name { get; set; } = ""; public string? Description { get; set; } public List<string> Permissions { get; set; } = []; public string Revision { get; set; } = ""; }
public class PermissionOverrideWrite { public string Code { get; set; } = ""; public bool Allowed { get; set; } }
public class AccountAccessWrite { public int IdRole { get; set; } public List<PermissionOverrideWrite> Overrides { get; set; } = []; public string Revision { get; set; } = ""; }
[Authorize(Roles = "Admin"), ApiController, Route("api/[controller]")]
public class AccessController(AppDbContext db) : ControllerBase
{
    static string Stamp(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    async Task<object> RoleDto(Role role) { var codes = await db.RolePermissions.Where(p => p.IdRole == role.Id).Select(p => p.Permission.Code).ToListAsync(); codes = codes.Where(c => DynamicAccess.Catalog.Any(r => r.Code == c)).OrderBy(x => x).ToList(); return new { role.Id, role.Name, role.Description, IsAdmin = role.Name == "Admin", Permissions = role.Name == "Admin" ? DynamicAccess.Catalog.Select(x => x.Code) : codes, Revision = Stamp(new { role.Name, role.Description, codes }), AccountCount = await db.Accounts.CountAsync(a => a.IdRole == role.Id) }; }
    async Task<object> AccountDto(Account account) { var changes = await db.AccountPermissions.Where(p => p.UserName == account.UserName).Select(p => new { p.Permission.Code, p.Allowed }).OrderBy(p => p.Code).ToListAsync(); return new { account.UserName, account.DisplayName, account.IdRole, account.IsActive, RoleName = await db.Roles.Where(r => r.Id == account.IdRole).Select(r => r.Name).SingleAsync(), Overrides = changes, Permissions = await DynamicAccess.Effective(db, account.UserName), Revision = Stamp(new { account.IdRole, changes }) }; }
    [HttpGet] public async Task<IActionResult> Get()
    {
        var roles = new List<object>(); foreach (var role in await db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync()) roles.Add(await RoleDto(role));
        var accounts = new List<object>(); foreach (var account in await db.Accounts.AsNoTracking().OrderBy(a => a.UserName).ToListAsync()) accounts.Add(await AccountDto(account));
        return Ok(new { catalog = DynamicAccess.Catalog, roles, accounts });
    }
    async Task Lock() => await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'Cafe.DynamicAccess', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, 'Cannot lock permissions', 1;");
    string? Validate(RoleAccessWrite dto) => string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length > 50 || dto.Description?.Length > 255 ? "Tên nhóm 1–50 ký tự, mô tả tối đa 255 ký tự." : ValidateCodes(dto.Permissions);
    static string? ValidateCodes(IEnumerable<string> codes) => codes.Any(c => !DynamicAccess.Catalog.Any(r => r.Code == c)) ? "Có mã quyền không hợp lệ." : null;
    static object? Revision(object dto) => dto.GetType().GetProperty("Revision")?.GetValue(dto);
    void Audit(string target, object changes) => db.AccessAudits.Add(new() { Actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", Target = target, Changes = JsonSerializer.Serialize(changes) });
    async Task SaveCodes(Role role, IEnumerable<string> codes) { db.RolePermissions.RemoveRange(await db.RolePermissions.Where(p => p.IdRole == role.Id).ToListAsync()); await db.SaveChangesAsync(); var selected = DynamicAccess.Normalize(codes); foreach (var p in await db.Permissions.Where(p => selected.Contains(p.Code)).ToListAsync()) db.RolePermissions.Add(new() { IdRole = role.Id, IdPermission = p.Id }); }
    [HttpPost("roles")] public async Task<IActionResult> CreateRole(RoleAccessWrite dto)
    {
        var error = Validate(dto); if (error != null) return BadRequest(new { message = error });
        if (dto.Name.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Admin là nhóm hệ thống." });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); await Lock();
        if (await db.Roles.AnyAsync(r => r.Name == dto.Name.Trim())) return BadRequest(new { message = "Tên nhóm đã tồn tại." });
        var role = new Role { Name = dto.Name.Trim(), Description = dto.Description?.Trim(), AccessConfigured = true }; db.Roles.Add(role); await db.SaveChangesAsync(); await SaveCodes(role, dto.Permissions); Audit("Nhóm " + role.Id, new { after = dto }); await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(await RoleDto(role));
    }
    [HttpPut("roles/{id:int}")] public async Task<IActionResult> UpdateRole(int id, RoleAccessWrite dto)
    {
        var error = Validate(dto); if (error != null) return BadRequest(new { message = error });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); await Lock();
        var role = await db.Roles.FindAsync(id); if (role == null) return NotFound();
        if (role.Name == "Admin" || dto.Name.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Admin luôn toàn quyền, không thể sửa nhóm này." });
        var before = await RoleDto(role); if (!Equals(Revision(before), dto.Revision)) return Conflict(new { message = "Nhóm đã thay đổi. Tải lại trước khi lưu." });
        if (await db.Roles.AnyAsync(r => r.Id != id && r.Name == dto.Name.Trim())) return BadRequest(new { message = "Tên nhóm đã tồn tại." });
        role.Name = dto.Name.Trim(); role.Description = dto.Description?.Trim(); await SaveCodes(role, dto.Permissions); Audit("Nhóm " + id, new { before, after = dto }); await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(await RoleDto(role));
    }
    [HttpPut("accounts/{username}")] public async Task<IActionResult> UpdateAccount(string username, AccountAccessWrite dto)
    {
        var error = ValidateCodes(dto.Overrides.Select(p => p.Code)); if (error != null || dto.Overrides.Select(p => p.Code).Distinct().Count() != dto.Overrides.Count) return BadRequest(new { message = error ?? "Mã quyền riêng bị lặp." });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); await Lock();
        var account = await db.Accounts.FindAsync(username); if (account == null) return NotFound(); var role = await db.Roles.FindAsync(dto.IdRole); if (role == null) return BadRequest(new { message = "Nhóm không tồn tại." });
        var before = await AccountDto(account); if (!Equals(Revision(before), dto.Revision)) return Conflict(new { message = "Quyền nhân viên đã thay đổi. Tải lại trước khi lưu." });
        error = await AccountController.Guard(db, account, User.FindFirstValue(ClaimTypes.NameIdentifier), role.Name != "Admin" && dto.IdRole != account.IdRole); if (error != null) return BadRequest(new { message = error });
        if (role.Name == "Admin" && dto.Overrides.Count > 0) return BadRequest(new { message = "Admin luôn toàn quyền, không có quyền riêng." });
        account.IdRole = dto.IdRole;
        db.AccountPermissions.RemoveRange(await db.AccountPermissions.Where(p => p.UserName == username).ToListAsync()); await db.SaveChangesAsync();
        var permissions = await db.Permissions.ToListAsync(); foreach (var change in dto.Overrides) db.AccountPermissions.Add(new() { UserName = username, IdPermission = permissions.Single(p => p.Code == change.Code).Id, Allowed = change.Allowed });
        Audit(username, new { before, after = dto }); await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(await AccountDto(account));
    }
    [HttpGet("audit")] public async Task<IActionResult> History() => Ok(await db.AccessAudits.AsNoTracking().OrderByDescending(a => a.Id).Take(100).ToListAsync());
}
