namespace Gateway.BLL.DTO.Response
{
    public class VOutboundAuthProfile : ListBase
    {
        public List<FOutboundAuthProfile> Data { get; set; } = [];
    }

    public class FOutboundAuthProfile : OutboundAuthProfile
    {
    }

    public class OutboundAuthHeader
    {
        public string Id { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string HeaderName { get; set; } = string.Empty;
        public string CredentialValue { get; set; } = string.Empty;
        public string SecondaryCredentialValue { get; set; } = string.Empty;
    }

    public class OutboundAuthProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<OutboundAuthHeader> Headers { get; set; } = [];
    }
}