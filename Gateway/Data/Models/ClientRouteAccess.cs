using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Client_Route_Access")]
    public class ClientRouteAccess
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public long EndpointId { get; set; }

        public bool IsAllowed { get; set; } = true;

        public int? OverrideRateLimit { get; set; }

        [StringLength(10)]
        public string? OverrideRatePeriod { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // Foreign Key Navigations
        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        [ForeignKey(nameof(EndpointId))]
        public virtual ApiEndpoint? ApiEndpoint { get; set; }
    }
}