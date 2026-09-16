using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Endpoint_IpRule")]
    public class EndpointIpRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long EndpointId { get; set; }

        [Required]
        [StringLength(100)]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string RuleType { get; set; } = "Allow";

        [StringLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(EndpointId))]
        public virtual ApiEndpoint? ApiEndpoint { get; set; }
    }
}