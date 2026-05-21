namespace Gateway.BLL.DTO.Response
{
    public class VRole : ListBase
    {
        public List<FRole> Data { get; set; } = [];
    }

    public class FRole : Role
    {
        public bool Deleted { get; set; }
    }
    public class Role : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    public class RoleModule
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Permissions { get; set; } = string.Empty;
        public bool Show { get; set; }
        public string Path { get; set; } = string.Empty;
        public string DisplayOrder { get; set; } = string.Empty;
        public string ModuleType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

    }
}
