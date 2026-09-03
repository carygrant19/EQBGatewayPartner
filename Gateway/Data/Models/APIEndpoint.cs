using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_ApiEndpoint")]
    public class ApiEndpoint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsWebSocket { get; set; } = false;
        public bool RequireApiKey { get; set; } = true;

        [StringLength(500)]
        public string UpstreamPathTemplate { get; set; } = string.Empty;

        [StringLength(100)]
        public string UpstreamHttpMethod { get; set; } = string.Empty;

        [StringLength(500)]
        public string DownstreamPathTemplate { get; set; } = string.Empty;

        [StringLength(10)]
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

        [StringLength(100)]
        public string? AllowedDays { get; set; }

        [StringLength(50)]
        public string LoadBalancingPolicy { get; set; } = "RoundRobin";

        public int TimeoutSeconds { get; set; } = 30;

        public bool EnableCaching { get; set; } = false;
        public int CacheTtlSeconds { get; set; } = 60;

        [StringLength(500)]
        public string AllowedOrigins { get; set; } = "*";

        public long? MaxRequestBodySize { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        public virtual ICollection<TargetHost> TargetHosts { get; set; } = [];
        public virtual ICollection<EndpointIpRule> IpRules { get; set; } = [];
    }
}