using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_Routes")]
    public class Route
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Stores the category string code matching Master_Route_Category.Code
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Navigation property referencing the category table joined on Code string keys
        /// </summary>
        [ForeignKey(nameof(Category))]
        public virtual RouteCategory? RouteCategory { get; set; }

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
        public string? DownstreamHttpVersion { get; set; } = "1.1";

        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }

        [StringLength(50)]
        public string? AuthenticationProviderKey { get; set; }

        public bool RouteIsCaseSensitive { get; set; }

        public int Priority { get; set; } = 1;

        public bool EnableRateLimiting { get; set; }

        public int? RateLimit { get; set; }

        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? RatePeriod { get; set; }

        public int? RatePeriodTimespan { get; set; }

        public int? RateLimitHttpStatusCode { get; set; } = 429;

        [StringLength(500)]
        public string? RateLimitQuotaExceededMessage { get; set; }

        public bool EnableCaching { get; set; }

        public int? CacheTtlSeconds { get; set; }

        public bool EnableQoS { get; set; }

        public int? QoSTimeoutMs { get; set; } = 30000;

        public int? QoSExceptionsAllowedBeforeBreaking { get; set; } = 3;

        public int? QoSDurationOfBreakMs { get; set; } = 10000;

        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string LoadBalancerType { get; set; } = "RoundRobin";

        [StringLength(100)]
        public string? LoadBalancerKey { get; set; }

        public int? LoadBalancerExpiryMs { get; set; } = 0;

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
        public string? ServiceNamespace { get; set; } // Fixed target field structural typo

        public bool EnableServicePolling { get; set; }

        public int? PollingIntervalMs { get; set; } = 300;

        // --- HTTP HANDLER CUSTOMIZATIONS ---
        public bool AllowAutoRedirect { get; set; }
        public bool UseCookieContainer { get; set; }
        public int MaxConnectionsPerServer { get; set; } = 100;

        // --- METADATA AUDIT TRACKERS ---
        public int CreatedBy { get; set; } // Matches DB Int Column types directly

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // --- CHILD RELATIONSHIPS COLLECTIONS ---
        public virtual ICollection<RouteIpRule> IpRules { get; set; } = new List<RouteIpRule>();
        public virtual ICollection<RouteHost> Hosts { get; set; } = new List<RouteHost>();
        public virtual ICollection<RouteAllowedScope> AllowedScopes { get; set; } = new List<RouteAllowedScope>();
        public virtual ICollection<RouteClient> Clients { get; set; } = new List<RouteClient>();
    }

    [Table("Master_Route_Category")]
    public class RouteCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; } = string.Empty;

        public bool? Deleted { get; set; } = false;
    }

    [Table("Map_Route_Clients")]
    public class RouteClient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public long RouteId { get; set; }

        [Required]
        [StringLength(50)]
        public string ClientId { get; set; } = string.Empty;

        [JsonIgnore]
        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route { get; set; }
    }

    [Table("Map_Route_IpRules")]
    public class RouteIpRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long? RouteId { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string RuleType { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route { get; set; }
    }

    [Table("Map_Route_Hosts")]
    public class RouteHost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long? RouteId { get; set; }

        [Required]
        [StringLength(255)]
        public string Host { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route { get; set; }
    }

    [Table("Map_Route_Allowed_Scopes")]
    public class RouteAllowedScope
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long RouteId { get; set; }

        [Required]
        [StringLength(150)]
        public string Scope { get; set; } = string.Empty;

        [JsonIgnore]
        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route { get; set; }
    }
}