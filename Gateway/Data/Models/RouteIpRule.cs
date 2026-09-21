using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Route_IpRule")]
    public class RouteIpRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long RouteId { get; set; }

        [Required]
        [StringLength(100)]
        public string IpAddressOrRange { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string RuleType { get; set; } = "Allow";

        [StringLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(RouteId))]
        public virtual Route? Route{ get; set; }
    }
}