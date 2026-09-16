namespace Gateway.BLL.DTO.Response
{
    public class APISecurityResult
    {
        public string? Status { get; set; } = string.Empty;
        public string? Key { get; set; } = string.Empty;
        public string? Secret { get; set; } = string.Empty;
    }

    public class VClient : ListBase
    {
        public List<FClient> Data { get; set; } = [];
    }

    public class FClient : Client
    {
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class Client
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string ApiKey { get; set; } = string.Empty;
        public bool SSLRequired { get; set; } = false;
        public bool Deleted { get; set; } = false;
    }
}