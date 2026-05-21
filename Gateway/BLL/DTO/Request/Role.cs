using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Role : Base
    {
        [Required(AllowEmptyStrings = true)]
        public string Id { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

        public List<RoleModulePermission>? RoleModulePermission { get; set; }
    }
    public class RoleModulePermission
    {
        public string? ModuleId { get; set; }
        public string[]? PermissionId { get; set; }
    }
}
