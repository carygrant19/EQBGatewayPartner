using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Route : Base
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsWebSocket { get; set; }

        [Required]
        [MaxLength(500)]
        public string UpstreamPathTemplate { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string UpstreamHttpMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string DownstreamPathTemplate { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string DownstreamScheme { get; set; } = "https";

        [MaxLength(255)]
        public string? UpstreamHost { get; set; }

        [MaxLength(10)]
        public string? DownstreamHttpVersion { get; set; } = "1.1";

        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }

        [MaxLength(50)]
        public string? AuthenticationProviderKey { get; set; }

        public bool RouteIsCaseSensitive { get; set; }

        public int Priority { get; set; } = 1;

        // --- RATE LIMITING STRATEGIES ---
        public bool EnableRateLimiting { get; set; }
        public int? RateLimit { get; set; }
        [MaxLength(10)]
        public string? RatePeriod { get; set; }
        public int? RatePeriodTimespan { get; set; }
        public int? RateLimitHttpStatusCode { get; set; } = 429;
        [MaxLength(500)]
        public string? RateLimitQuotaExceededMessage { get; set; }

        // --- CACHING STRATEGIES ---
        public bool EnableCaching { get; set; }
        public int? CacheTtlSeconds { get; set; }

        // --- QUALITY OF SERVICE (QoS) CONFIGS ---
        public bool EnableQoS { get; set; }
        public int? QoSTimeoutMs { get; set; } = 30000;
        public int? QoSExceptionsAllowedBeforeBreaking { get; set; } = 3;
        public int? QoSDurationOfBreakMs { get; set; } = 10000;

        // --- LOAD BALANCER PROPERTIES ---
        [Required]
        [MaxLength(50)]
        public string LoadBalancerType { get; set; } = "RoundRobin";
        [MaxLength(100)]
        public string? LoadBalancerKey { get; set; }
        public int? LoadBalancerExpiryMs { get; set; }

        // --- POLICY & TIME BOUNDARY ENFORCEMENTS ---
        public bool RequireSignature { get; set; }
        public bool EnableTimeLimit { get; set; }
        [MaxLength(5)]
        public string? TimeFrom { get; set; }
        [MaxLength(5)]
        public string? TimeTo { get; set; }
        [MaxLength(50)]
        public string? AllowedDays { get; set; }

        // --- SERVICE DISCOVERY CONFIGS ---
        public bool UseServiceDiscovery { get; set; }
        [MaxLength(255)]
        public string? ServiceName { get; set; }
        [MaxLength(255)]
        public string? ServiceNamespace;
        public bool EnableServicePolling { get; set; }
        public int? PollingIntervalMs { get; set; } = 300;

        // --- CHILD RELATIONAL COLLECTIONS ---
        public List<RouteHost>? Hosts { get; set; } = [];
        public List<RouteIpRule>? IpRules { get; set; } = [];
    }

    public class RouteHost
    {
        [Required]
        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }
    }

    public class RouteIpRule
    {
        [Required]
        [MaxLength(100)]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string RuleType { get; set; } = "Allow"; // Allow or Deny
    }
}