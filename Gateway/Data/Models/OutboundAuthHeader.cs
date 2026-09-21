using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_OutboundAuthHeader")]
    public class OutboundAuthHeader
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ProfileId { get; set; }

        [Required]
        [StringLength(50)]
        public string AuthType { get; set; } = "CustomHeader"; // 'ApiKey', 'BasicAuth', 'BearerToken', 'CustomHeader'

        [StringLength(100)]
        public string? HeaderName { get; set; }

        [Required]
        public string CredentialValue { get; set; } = string.Empty;

        public string? SecondaryCredentialValue { get; set; }

        [ForeignKey("ProfileId")]
        public virtual OutboundAuthProfile? Profile { get; set; }
    }
}