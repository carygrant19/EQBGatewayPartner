using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_Api_Client")]
    public class ApiClient
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public bool SSLRequired { get; set; } = false;
        public bool Deleted { get; set; } = false; 
        public int? CreatedBy { get; set; } = default!; 
        public DateTime? CreatedDate { get; set; } = DateTime.Now; 
        public int? UpdatedBy { get; set; } = default!; 
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
    }
}
