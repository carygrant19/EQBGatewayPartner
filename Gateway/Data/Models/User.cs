using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("user")]
    public class User : Base
    {
        [Key]
        public int Id { get; set; } = 0;
        public bool? LDAPAuthentication { get; set; } = false;
        public string? Username { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public int? BranchId { get; set; } = 0;
        public int? DivisionId { get; set; } = 0;
        public int? DepartmentId { get; set; } = 0;
        public string? FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? PhoneNo { get; set; } = string.Empty;
        public string? MobileNo { get; set; } = string.Empty;
        public int? Status { get; set; } = 0;
        public int? PasswordAttempt { get; set; } = 0;
        public DateTime? PasswordExpirationDate { get; set; }
        public DateTime? PasswordLastChange { get; set; }
        public bool? DefaultPassword { get; set; } = false;
        public byte[]? ImageContent { get; set; }
        public byte[]? ImageContentThumbnail { get; set; }
        public string? ImageType { get; set; } = string.Empty;

        [NotMapped]
        public Branch? Branch { get; set; }
        public List<UserRole>? UserRoles { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public bool Deleted { get; set; } = false;
        [NotMapped]
        public string? FullName { get; set; } = string.Empty;
    }
    [Table("master_active_user")]
    public class ActiveUser
    {
        [Key]
        public int? Id { get; set; }
        public int? UserId { get; set; } = 0;
        public string? Terminal { get; set; } = string.Empty;
        public DateTime? ActivityDate { get; set; }

    }

    public class UserPassword : Base
    {
        public string Username { get; set; }
        public string New { get; set; }
        public string Current { get; set; }
        public string Confirm { get; set; }

    }
}
