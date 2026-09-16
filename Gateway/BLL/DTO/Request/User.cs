using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class User : Base
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string LDAPAuthentication { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string? Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string MiddleName { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Email { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Branch { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string EncryptedRoleId { get; set; } = string.Empty;

        public byte[]? ImageContent { get; set; }
        public string? ImageType { get; set; } = string.Empty;
        public List<UserRole>? UserRoles { get; set; }
    }
    public class UserRole
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleDesc { get; set; } = string.Empty;
    }
    public class Credential : Base
    {
        [Required(ErrorMessage = "User Id is required")]
        public string Username { get; set; } = string.Empty!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty!;
    }
    public class UserPassword : Base
    {
        public string? Username { get; set; }
        public string? New { get; set; }
        public string? Current { get; set; }
        public string? Confirm { get; set; }

    }
}
