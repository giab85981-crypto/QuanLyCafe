using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: Lấy danh sách toàn bộ tài khoản
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var accounts = await _context.Accounts
                    .Include(a => a.Role)
                    .Select(a => new AccountDto
                    {
                        UserName = a.UserName,
                        DisplayName = a.DisplayName,
                        IdRole = a.IdRole,
                        RoleName = a.Role.Name,
                        IsActive = a.IsActive
                    })
                    .ToListAsync();

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách tài khoản!", detail = ex.Message });
            }
        }

        // 2. POST: Tạo tài khoản mới
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.PassWord))
                {
                    return BadRequest(new { message = "Tên đăng nhập và Mật khẩu không được để trống!" });
                }

                var existing = await _context.Accounts.FindAsync(dto.UserName);
                if (existing != null)
                {
                    return BadRequest(new { message = "Tên tài khoản này đã tồn tại!" });
                }

                var role = await _context.Roles.FindAsync(dto.IdRole);
                if (role == null)
                {
                    return BadRequest(new { message = "Vai trò (Role) không hợp lệ!" });
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.PassWord);

                var account = new Account
                {
                    UserName = dto.UserName,
                    PassWord = hashedPassword,
                    DisplayName = dto.DisplayName,
                    IdRole = dto.IdRole,
                    IsActive = true
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Tạo tài khoản thành công!", userName = account.UserName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo tài khoản!", detail = ex.Message });
            }
        }

        // 3. PUT: Cập nhật thông tin tài khoản (Tên hiển thị, Role, Mật khẩu mới nếu có)
        [HttpPut("{username}")]
        public async Task<IActionResult> Update(string username, [FromBody] UpdateAccountDto dto)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(username);
                if (account == null)
                {
                    return NotFound(new { message = "Không tìm thấy tài khoản cần cập nhật!" });
                }

                var role = await _context.Roles.FindAsync(dto.IdRole);
                if (role == null)
                {
                    return BadRequest(new { message = "Vai trò (Role) không hợp lệ!" });
                }

                account.DisplayName = dto.DisplayName;
                account.IdRole = dto.IdRole;

                // Nếu truyền mật khẩu mới thì tiến hành băm và cập nhật
                if (!string.IsNullOrWhiteSpace(dto.PassWord))
                {
                    account.PassWord = BCrypt.Net.BCrypt.HashPassword(dto.PassWord);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật thông tin tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông tin tài khoản!", detail = ex.Message });
            }
        }

        // 4. PUT: Khóa / Mở khóa tài khoản (IsActive)
        [HttpPut("{username}/status")]
        public async Task<IActionResult> UpdateStatus(string username, [FromBody] UpdateAccountStatusDto dto)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(username);
                if (account == null)
                {
                    return NotFound(new { message = "Không tìm thấy tài khoản!" });
                }

                account.IsActive = dto.IsActive;
                await _context.SaveChangesAsync();

                string statusText = account.IsActive ? "kích hoạt" : "khóa";
                return Ok(new { message = $"Đã {statusText} tài khoản thành công!", isActive = account.IsActive });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái tài khoản!", detail = ex.Message });
            }
        }

        // 5. DELETE: Xóa tài khoản (Kiểm tra ràng buộc CSDL)
        [HttpDelete("{username}")]
        public async Task<IActionResult> Delete(string username)
        {
            try
            {
                var account = await _context.Accounts.FindAsync(username);
                if (account == null)
                {
                    return NotFound(new { message = "Không tìm thấy tài khoản cần xóa!" });
                }

                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa tài khoản thành công!" });
            }
            catch (DbUpdateException)
            {
                // Bắt lỗi ràng buộc khóa ngoại Foreign Key khi tài khoản đã phát sinh dữ liệu liên quan
                return BadRequest(new { message = "Tài khoản này đã phát sinh lịch sử giao dịch/hóa đơn trong hệ thống nên không thể xóa! Vui lòng chuyển sang dùng chức năng 'Khóa tài khoản' để đảm bảo an toàn CSDL." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa tài khoản!", detail = ex.Message });
            }
        }
    }
}