namespace Gateway.BLL.DTO.Response
{
    public class VApiEndpoint : ListBase
    {
        public List<FApiEndpoint> Data { get; set; } = [];
    }

    public class FApiEndpoint : ApiEndpoint
    {
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<FTargetHost> TargetHosts { get; set; } = [];
        public List<FEndpointIpRule> IpRules { get; set; } = [];
    }

    public class ApiEndpoint
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public bool IsActive { get; set; }
        public bool IsWebSocket { get; set; }
        public bool RequireApiKey { get; set; } = true;

        public string UpstreamPathTemplate { get; set; } = string.Empty;
        public string UpstreamHttpMethod { get; set; } = string.Empty;
        public string DownstreamPathTemplate { get; set; } = string.Empty;
        public string DownstreamScheme { get; set; } = "https";
        public int Priority { get; set; }

        public bool EnableRateLimiting { get; set; }
        public int? RateLimit { get; set; }
        public string? RatePeriod { get; set; }
        public int? RatePeriodTimespan { get; set; }

        public TimeSpan? TimeFrom { get; set; }
        public TimeSpan? TimeTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? AllowedDays { get; set; }

        public string LoadBalancingPolicy { get; set; } = "RoundRobin";
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableCaching { get; set; }
        public int CacheTtlSeconds { get; set; }
        public string AllowedOrigins { get; set; } = "*";
        public long? MaxRequestBodySize { get; set; }
    }

    public class FTargetHost
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public int Weight { get; set; }
        public string? Description { get; set; }
    }

    public class FEndpointIpRule
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string IpAddressOrRange { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}