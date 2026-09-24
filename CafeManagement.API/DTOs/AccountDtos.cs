namespace CafeManagement.API.DTOs
{
    public class AccountDto
    {
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int IdRole { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateAccountDto
    {
        public string UserName { get; set; } = string.Empty;
        public string PassWord { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int IdRole { get; set; }
    }

    public class UpdateAccountDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public int IdRole { get; set; }
        public string? PassWord { get; set; } // Nếu truyền vào sẽ cập nhật mật khẩu mới
    }

    public class UpdateAccountStatusDto
    {
        public bool IsActive { get; set; }
    }
}