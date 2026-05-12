using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("master_permission")]
    public class Permission : Base
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public ICollection<RoleModulePermission>? RoleModulePermissions { get; set; }
        public bool Deleted { get; set; } = false;
    }
}
