namespace Gateway.BLL.DTO.Response
{
    public class VActivityLog : ListBase
    {
        public List<FActivityLog> Data { get; set; } = [];
    }

    public class FActivityLog : ActivityLog
    {
        public bool Deleted { get; set; }
    }
    public class ActivityLog
    {
        public string Id { get; set; }
        public int UserId { get; set; } = 0!;
        public string ModuleName { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string Details { get; set; } = default!;
        public DateTime LogDate { get; set; } = DateTime.Now;
    }
}
