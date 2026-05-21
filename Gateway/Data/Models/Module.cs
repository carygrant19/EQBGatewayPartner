using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_Module")]
    public class Module : Base
    {
        [Key]
        public int Id { get; set; }
        public string? Code { get; set; } = string.Empty;
        public string? Name { get; set; } = string.Empty;
        public string? ModuleType { get; set; } = string.Empty;
        public int? DisplayOrder { get; set; }
        public int? ParentId { get; set; }

        public string? Description { get; set; } = string.Empty;
        public string? Url { get; set; } = string.Empty;
        public string? Icon { get; set; } = string.Empty;
        public string? AuditContent { get; set; } = string.Empty;
        public bool Show { get; set; } = false;

        [ForeignKey("ParentId")]
        public Module? Parent { get; set; }
        public ICollection<Module>? Children { get; set; }
        public ICollection<ModulePermission>? ModulePermission { get; set; }
        public ICollection<RoleModulePermission>? RoleModulePermissions { get; set; }

        public bool Deleted { get; set; } = false;
    }

    [Table("Map_Module_Permission")]
    public class ModulePermission
    {
        public long Id { get; set; }
        public int ModuleId { get; set; }
        public int PermissionId { get; set; }

        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }

        [ForeignKey("ModuleId")]
        public Module? Module { get; set; }
    }

    public class ModuleProperty
    {
        public string Url { get; set; } = string.Empty;
        public string Permission { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
