using System;
using System.Collections.Generic;

namespace Gateway.BLL.DTO.Response
{
    public class VRoute : ListBase
    {
        public List<FRoute> Data { get; set; } = [];
    }

    public class FRoute : Route
    {
        // Includes nested relational tracking maps for grid displays and filtration controls
        public List<FRouteHost> Hosts { get; set; } = [];
        public List<FRouteIpRule> IpRules { get; set; } = [];
    }

    public class Route : Base
    {
        public string Id { get; set; } = string.Empty; // Expressed as explicit string keys per your SQL schema
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsWebSocket { get; set; }
        public string UpstreamPathTemplate { get; set; } = string.Empty;
        public string UpstreamHttpMethod { get; set; } = string.Empty;
        public string DownstreamPathTemplate { get; set; } = string.Empty;
        public string DownstreamScheme { get; set; } = string.Empty;
        public string? UpstreamHost { get; set; }
        public string? DownstreamHttpVersion { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
        public string? AuthenticationProviderKey { get; set; }
        public bool RouteIsCaseSensitive { get; set; }
        public int Priority { get; set; }

        // --- RATE LIMITING PROPERTIES ---
        public bool EnableRateLimiting { get; set; }
        public int? RateLimit { get; set; }
        public string? RatePeriod { get; set; }
        public int? RatePeriodTimespan { get; set; }
        public int? RateLimitHttpStatusCode { get; set; }
        public string? RateLimitQuotaExceededMessage { get; set; }

        // --- CACHING PROPERTIES ---
        public bool EnableCaching { get; set; }
        public int? CacheTtlSeconds { get; set; }

        // --- QUALITY OF SERVICE (QoS) PROPERTIES ---
        public bool EnableQoS { get; set; }
        public int? QoSTimeoutMs { get; set; }
        public int? QoSExceptionsAllowedBeforeBreaking { get; set; }
        public int? QoSDurationOfBreakMs { get; set; }

        // --- LOAD BALANCER PROPERTIES ---
        public string LoadBalancerType { get; set; } = "RoundRobin";
        public string? LoadBalancerKey { get; set; }
        public int? LoadBalancerExpiryMs { get; set; }

        // --- ADDITIONAL POLICY SECURITY CONFIGS ---
        public bool RequireSignature { get; set; }
        public bool EnableTimeLimit { get; set; }
        public string? TimeFrom { get; set; }
        public string? TimeTo { get; set; }
        public string? AllowedDays { get; set; }

        // --- SERVICE DISCOVERY PROPERTIES ---
        public bool UseServiceDiscovery { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceNamespace { get; set; }
        public bool EnableServicePolling { get; set; }
        public int? PollingIntervalMs { get; set; }
    }

    public class FRouteHost
    {
        public int Id { get; set; }
        public string? RouteId { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class FRouteIpRule
    {
        public int Id { get; set; }
        public string? RouteId { get; set; }
        public string IpAddressOrRange { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty; // Allow or Deny
    }
}