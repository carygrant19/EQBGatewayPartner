namespace Gateway.BLL.DTO.Response
{
    public class APISecurityResult
    {
        public string? Status { get; set; } = string.Empty;
        public string? Key { get; set; } = string.Empty;
        public string? Secret { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
    }
    public class VClient : ListBase
    {
        public List<FClient> Data { get; set; } = [];
    }

    public class FClient : Client
    {
        public bool Deleted { get; set; }
    }
    public class Client : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public bool SSLRequired { get; set; } = false;
        public bool Deleted { get; set; } = false;
    }
}


