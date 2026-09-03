using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Map_Client_Credential")]
    public class ClientCredential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ClientId { get; set; }

        [Required]
        [StringLength(50)]
        public string KeyType { get; set; } = "Primary"; // Primary, Secondary

        [Required]
        [StringLength(100)]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ApiSecretHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }
    }
}