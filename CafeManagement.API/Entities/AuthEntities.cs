using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.API.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class Permission
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        public int IdRole { get; set; }
        [ForeignKey("IdRole")]
        public Role Role { get; set; } = null!;

        public int IdPermission { get; set; }
        [ForeignKey("IdPermission")]
        public Permission Permission { get; set; } = null!;
    }

    public class Account
    {
        [Key]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        public string PassWord { get; set; } = string.Empty;

        public int IdRole { get; set; }
        [ForeignKey("IdRole")]
        public Role Role { get; set; } = null!;

        public ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
        public bool IsActive { get; internal set; }
    }
}