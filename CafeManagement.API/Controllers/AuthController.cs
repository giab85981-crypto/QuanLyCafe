using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CafeManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // 1. Tìm tài khoản trong CSDL
            var account = await _context.Accounts
                .Include(a => a.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(a => a.UserName == request.UserName);

            if (account == null)
            {
                return BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            // 2. Kiểm tra mật khẩu mã hóa BCrypt
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(request.PassWord, account.PassWord);
            if (!isValidPassword)
            {
                return BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            // 3. Lấy danh sách mã quyền (Permission Codes)
            var permissions = account.Role.RolePermissions
                .Select(rp => rp.Permission.Code)
                .ToList();

            // 4. Tạo JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "SecretKeyCoreCafeManagementProject2026StrongEnough!");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.UserName),
                new Claim(ClaimTypes.Name, account.DisplayName),
                new Claim(ClaimTypes.Role, account.Role.Name)
            };

            // Add các quyền vào Claim
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("Permission", perm));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 5. Trả kết quả về Frontend
            return Ok(new LoginResponseDto
            {
                Token = tokenHandler.WriteToken(token),
                UserName = account.UserName,
                DisplayName = account.DisplayName,
                RoleName = account.Role.Name,
                Permissions = permissions
            });
        }
    }
}