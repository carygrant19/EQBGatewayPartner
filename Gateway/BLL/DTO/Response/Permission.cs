namespace Gateway.BLL.DTO.Response
{
    public class VPermission : ListBase
    {
        public List<FPermission> Data { get; set; } = [];
    }
    public class FPermission : Permission
    {
        public bool Deleted { get; set; }
    }
    public class Permission
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

    }
}
