using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Route_TargetHost")]
    public class TargetHost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long RouteId { get; set; }

        [Required]
        [StringLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public int Weight { get; set; } = 1;

        [StringLength(255)]
        public string? Description { get; set; }

        // --- HEALTH CHECK FEATURES ---
        [StringLength(255)]
        public string? HealthCheckPath { get; set; }

        public bool IsHealthy { get; set; } = true;

        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route{ get; set; }
    }
}