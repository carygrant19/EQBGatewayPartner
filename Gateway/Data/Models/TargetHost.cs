using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_TargetHost")]
    public class TargetHost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long EndpointId { get; set; }

        [Required]
        [StringLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public int Weight { get; set; } = 1;

        [StringLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(EndpointId))]
        public virtual ApiEndpoint? ApiEndpoint { get; set; }
    }
}