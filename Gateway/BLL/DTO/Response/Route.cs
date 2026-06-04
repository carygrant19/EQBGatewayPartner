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
        public List<FRouteHost> Hosts { get; set; } = [];
        public List<FRouteIpRule> IpRules { get; set; } = [];
        public List<FRouteClient> Clients { get; set; } = [];
    }

    public class Route : Base
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Category { get; set; } = string.Empty;
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

        public bool EnableRateLimiting { get; set; }
        public int? RateLimit { get; set; }
        public string? RatePeriod { get; set; }
        public int? RatePeriodTimespan { get; set; }
        public int? RateLimitHttpStatusCode { get; set; }
        public string? RateLimitQuotaExceededMessage { get; set; }

        public bool EnableCaching { get; set; }
        public int? CacheTtlSeconds { get; set; }

        public bool EnableQoS { get; set; }
        public int? QoSTimeoutMs { get; set; }
        public int? QoSExceptionsAllowedBeforeBreaking { get; set; }
        public int? QoSDurationOfBreakMs { get; set; }

        public string LoadBalancerType { get; set; } = "RoundRobin";
        public string? LoadBalancerKey { get; set; }
        public int? LoadBalancerExpiryMs { get; set; }

        public bool RequireSignature { get; set; }
        public bool EnableTimeLimit { get; set; }
        public string? TimeFrom { get; set; }
        public string? TimeTo { get; set; }
        public string? AllowedDays { get; set; }

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
        public string Description { get; set; } = string.Empty;
    }

    public class FRouteIpRule
    {
        public int Id { get; set; }
        public string? RouteId { get; set; }
        public string IpAddressOrRange { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class FRouteClient
    {
        public long Id { get; set; }
        public string? RouteId { get; set; }
        public string ClientId { get; set; } = string.Empty;
    }
}