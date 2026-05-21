using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.BLL.DTO.Response
{
    public class VModule : ListBase
    {
        public List<FModule> Data { get; set; } = [];
    }

    public class FModule : Module
    {
        public bool Deleted { get; set; }
    }
    public class Module : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModuleType { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string DisplayOrder { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string AuditContent { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool Show { get; set; }
        public string ModulePermission { get; set; } = string.Empty;
    }

    public class ModuleAccess : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModuleType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string DisplayOrder { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
        public string AuditContent { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool Show { get; set; }
        public string ModuleGroup { get; set; } = string.Empty;
        public string ModuleGroupDesc { get; set; } = string.Empty;
        public int ModuleGroupDisplayOrder { get; set; }
    }
    public class ModulePermission
    {
        public string Id { get; set; } = string.Empty;
        public string ModuleId { get; set; } = string.Empty;
        public string PermissionId { get; set; } = string.Empty;

        public string ModuleDesc { get; set; } = string.Empty;
        public string PermissionDesc { get; set; } = string.Empty;

        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }
    }
}
