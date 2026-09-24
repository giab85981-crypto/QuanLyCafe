namespace CafeManagement.API.DTOs
{
    public class LoginRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string PassWord { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
    }
}