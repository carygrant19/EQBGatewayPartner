using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace Gateway.Data.Models
{
    [Table("Master_OutboundAuthProfile")]
    public class OutboundAuthProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Navigation Property: Headers inside this profile (One-to-Many)
        public virtual ICollection<OutboundAuthHeader> Headers { get; set; } = new List<OutboundAuthHeader>();

        // Navigation Property: Routes using this profile
        public virtual ICollection<Route> Endpoints { get; set; } = new List<Route>();
    }
}