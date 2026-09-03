using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class ApiEndpoint : Base
    {
        public string Id { get; set; } = "0";

        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsWebSocket { get; set; } = false;
        public bool RequireApiKey { get; set; } = true;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(500)]
        public string UpstreamPathTemplate { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string UpstreamHttpMethod { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(500)]
        public string DownstreamPathTemplate { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(10)]
        public string DownstreamScheme { get; set; } = "https";

        public int Priority { get; set; } = 1;

        public bool EnableRateLimiting { get; set; } = false;
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
        public bool EnableCaching { get; set; } = false;
        public int CacheTtlSeconds { get; set; } = 60;
        public string AllowedOrigins { get; set; } = "*";
        public long? MaxRequestBodySize { get; set; }

        public List<TargetHost> TargetHosts { get; set; } = [];
        public List<EndpointIpRule> IpRules { get; set; } = [];
    }

    public class TargetHost
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }
        public int Weight { get; set; } = 1;

        [MaxLength(255)]
        public string? Description { get; set; }
    }

    public class EndpointIpRule
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(10)]
        public string RuleType { get; set; } = "Allow";

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}