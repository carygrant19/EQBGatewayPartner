using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Route : Base
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
        public int? AuthProviderId { get; set; }
        public int? OutboundAuthProfileId { get; set; }

        public List<int> ClientIds { get; set; } = []; // <-- IDINAGDAG PARA SA AUTHORIZED CLIENTS

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

        // --- OPERATING HOURS & EFFECTIVE DATES ---
        public TimeSpan? TimeFrom { get; set; }
        public TimeSpan? TimeTo { get; set; }
        public DateTime? DateFrom { get; set; } // <-- IDINAGDAG
        public DateTime? DateTo { get; set; }   // <-- IDINAGDAG
        public string? AllowedDays { get; set; }

        public string LoadBalancingPolicy { get; set; } = "RoundRobin";
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableCaching { get; set; } = false;
        public int CacheTtlSeconds { get; set; } = 60;
        public string AllowedOrigins { get; set; } = "*";
        public long? MaxRequestBodySize { get; set; }

        // --- ENTERPRISE GATEWAY FEATURES ---
        [Required(AllowEmptyStrings = false)]
        [MaxLength(20)]
        public string IntegrationType { get; set; } = "PROXY";

        public bool StripPath { get; set; } = true;
        public bool PreserveHostHeader { get; set; } = false;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string AllowedMethods { get; set; } = "GET,POST,PUT,DELETE";

        [MaxLength(20)]
        public string? ApiVersion { get; set; } = "v1";

        public int MaxRetries { get; set; } = 0;
        public int RetryDelayMs { get; set; } = 1000;
        public bool EnableCircuitBreaker { get; set; } = false;

        public int? MockResponseCode { get; set; }
        public string? MockResponseBody { get; set; }

        public List<TargetHost> TargetHosts { get; set; } = [];
        public List<RouteIpRule> IpRules { get; set; } = [];
        public List<RouteTransform> Transforms { get; set; } = [];
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

        [MaxLength(255)]
        public string? HealthCheckPath { get; set; }

        public bool IsHealthy { get; set; } = true;
    }

    public class RouteIpRule
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

    public class RouteTransform
    {
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(20)]
        public string TransformPhase { get; set; } = "Request";

        [Required(AllowEmptyStrings = false)]
        [MaxLength(20)]
        public string Action { get; set; } = "Add";

        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        public string HeaderName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? HeaderValue { get; set; }
    }
}