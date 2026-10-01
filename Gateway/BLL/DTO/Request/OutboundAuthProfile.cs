namespace Gateway.BLL.DTO.Request
{
    public class OutboundAuthHeader
    {
        public string Id { get; set; } = string.Empty;
        public string AuthType { get; set; } = "APIKey";
        public string HeaderName { get; set; } = "X-Api-Key";
        public string CredentialValue { get; set; } = string.Empty;
        public string SecondaryCredentialValue { get; set; } = string.Empty;
    }

    public class OutboundAuthProfile : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<OutboundAuthHeader> Headers { get; set; } = [];
    }
}
 