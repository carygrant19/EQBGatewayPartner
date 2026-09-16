using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Endpoint_Transform")]
    public class EndpointTransform
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long EndpointId { get; set; }

        [Required]
        [StringLength(20)]
        public string TransformPhase { get; set; } = string.Empty; // "Request" o "Response"

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // "Add", "Remove", "Append"

        [Required]
        [StringLength(100)]
        public string HeaderName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? HeaderValue { get; set; }

        [ForeignKey(nameof(EndpointId))]
        public virtual ApiEndpoint? ApiEndpoint { get; set; }
    }
}