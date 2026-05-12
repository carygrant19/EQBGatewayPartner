using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gateway.BLL.DTO.Response
{
    public class VUser : ListBase
    {
        public List<FUser> Data { get; set; } = [];
    }

    public class FUser : User { }
    public class User : Base
    {
        public string Id { get; set; } = string.Empty;
        public bool? LDAPAuthentication { get; set; } = false;
        public Branch? Branch { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public int? PasswordAttempt { get; set; } = 0;
        public DateTime? PasswordExpirationDate { get; set; }
        public DateTime? PasswordLastChange { get; set; }
        public bool? IsLocked { get; set; } = false;
        public bool? DefaultPassword { get; set; } = false;
        public bool? Deleted { get; set; } = false;
        public byte[]? ImageContent { get; set; }
        public byte[]? ImageContentThumbnail { get; set; }
        //public string? ImageContentBase64 { get; set; }
        public string? ImageType { get; set; } = string.Empty;

        public List<UserRole>? UserRoles { get; set; }

        [NotMapped]
        [JsonIgnore]
        public Result Result { get; set; }
        [NotMapped]
        [JsonIgnore]
        public string LDAPPath { get; set; } = string.Empty;

    }
    public class UserRole
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public string RoleDesc { get; set; } = string.Empty;

    }

}
