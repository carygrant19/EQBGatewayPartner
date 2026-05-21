using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Routes")]
    public class Route
    {
        [Key]
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public bool IsWebSocket { get; set; }

        [Required]
        [StringLength(500)]
        public string UpstreamPathTemplate { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string UpstreamHttpMethod { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DownstreamPathTemplate { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string DownstreamScheme { get; set; } = string.Empty;

        [StringLength(255)]
        public string? UpstreamHost { get; set; }

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? DownstreamHttpVersion { get; set; }

        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }

        [StringLength(50)]
        public string? AuthenticationProviderKey { get; set; }

        public bool RouteIsCaseSensitive { get; set; }

        public int Priority { get; set; }
         
        public bool EnableRateLimiting { get; set; }

        public int? RateLimit { get; set; }

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? RatePeriod { get; set; }

        public int? RatePeriodTimespan { get; set; }

        public int? RateLimitHttpStatusCode { get; set; }

        [StringLength(500)]
        public string? RateLimitQuotaExceededMessage { get; set; } 
        public bool EnableCaching { get; set; }

        public int? CacheTtlSeconds { get; set; } 
        public bool EnableQoS { get; set; }

        public int? QoSTimeoutMs { get; set; }

        public int? QoSExceptionsAllowedBeforeBreaking { get; set; }

        public int? QoSDurationOfBreakMs { get; set; } 
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string LoadBalancerType { get; set; } = "RoundRobin";

        [StringLength(100)]
        public string? LoadBalancerKey { get; set; }

        public int? LoadBalancerExpiryMs { get; set; }
         
        public bool RequireSignature { get; set; }

        public bool EnableTimeLimit { get; set; }

        [StringLength(5)]
        [Column(TypeName = "varchar(5)")]
        public string? TimeFrom { get; set; }

        [StringLength(5)]
        [Column(TypeName = "varchar(5)")]
        public string? TimeTo { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? AllowedDays { get; set; }

        // --- SERVICE DISCOVERY ---
        public bool UseServiceDiscovery { get; set; }

        [StringLength(255)]
        public string? ServiceName { get; set; }

        [StringLength(255)]
        public string? ServiceNamespace;

        public bool EnableServicePolling { get; set; }

        public int? PollingIntervalMs { get; set; }

        // --- METADATA ---
        [Required]
        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // --- RELATIONSHIPS ---
        public virtual ICollection<RouteIpRule> IpRules { get; set; } = new List<RouteIpRule>();
        public virtual ICollection<RouteHost> Hosts { get; set; } = new List<RouteHost>();
    }


    [Table("Route_IpRules")]
    public class RouteIpRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? RouteId { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string RuleType { get; set; } = string.Empty; // e.g., Allow/Deny

        [ForeignKey("RouteId")]
        public virtual Route? Route { get; set; }
    }

    [Table("Route_Hosts")]
    public class RouteHost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? RouteId { get; set; }

        [Required]
        [StringLength(255)]
        public string Host { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [ForeignKey("RouteId")]
        public virtual Route? Route { get; set; }
    }
 
    [Table("GlobalConfiguration")]
    public class GlobalConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ClientIdHeader { get; set; } = "X-Api-Key";

        [StringLength(500)]
        public string? QuotaExceededMessage { get; set; }

        [Required]
        public int RateLimitHttpStatusCode { get; set; }

        [Required]
        public bool EnableRateLimitHeaders { get; set; }

        [StringLength(500)]
        public string? BaseUrl { get; set; }

        [StringLength(100)]
        public string? RequestIdKey { get; set; }

        [Required]
        public DateTime UpdatedDate { get; set; }

        [Required]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogLevel { get; set; } = "Error";

        [Required]
        public bool EnableRequestId { get; set; }
    }
     
}