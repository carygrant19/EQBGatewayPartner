namespace Gateway.BLL.DTO.Response
{
    public class VAccountMapping : ListBase
    {
        public List<FAccountMapping> Data { get; set; } = [];
    }
    public class FAccountMapping : AccountMapping
    {
        public bool Deleted { get; set; }
    }
    public class AccountMapping
    {
        public string? ID { get; set; } = string.Empty;
        public string? BranchId { get; set; } = string.Empty;
        public string? CompanyId { get; set; } = string.Empty;
        public string? InternalAccountNo { get; set; } = string.Empty;
        public string? Recipient { get; set; } = string.Empty;

    }
}
