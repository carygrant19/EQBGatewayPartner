namespace Gateway.BLL.DTO.Response
{
    public class VRoute : ListBase
    {
        public List<FRoute> Data { get; set; } = [];
    }

    public class FRoute : Route
    {
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public List<FTargetHost> TargetHosts { get; set; } = [];
        public List<FRouteIpRule> IpRules { get; set; } = [];
        public List<FRouteTransform> Transforms { get; set; } = [];
    }

    public class Route
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public int? AuthProviderId { get; set; }
        public string? AuthProviderName { get; set; }

        public int? OutboundAuthProfileId { get; set; } // <-- Idinagdag
        public string? OutboundAuthProfileName { get; set; } // <-- Idinagdag para sa display sa UI

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

        // --- ENTERPRISE GATEWAY FEATURES ---
        public string IntegrationType { get; set; } = "PROXY";
        public bool StripPath { get; set; } = true;
        public bool PreserveHostHeader { get; set; } = false;
        public string AllowedMethods { get; set; } = "GET,POST,PUT,DELETE";
        public string? ApiVersion { get; set; } = "v1";
        public int MaxRetries { get; set; } = 0;
        public int RetryDelayMs { get; set; } = 1000;
        public bool EnableCircuitBreaker { get; set; } = false;
        public int? MockResponseCode { get; set; }
        public string? MockResponseBody { get; set; }
    }

    public class FTargetHost
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public int Weight { get; set; }
        public string? Description { get; set; }
        public string? HealthCheckPath { get; set; }
        public bool IsHealthy { get; set; } = true;
    }

    public class FRouteIpRule
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string IpAddressOrRange { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class FRouteTransform
    {
        public int Id { get; set; }
        public string EndpointId { get; set; } = string.Empty;
        public string TransformPhase { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string HeaderName { get; set; } = string.Empty;
        public string? HeaderValue { get; set; }
    }
}