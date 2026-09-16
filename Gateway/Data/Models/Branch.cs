using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_Branch")]
    public class Branch : Base
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? BankingHour { get; set; } = string.Empty;
        public string? Officer { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? ContactNo { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public bool Deleted { get; set; } = false;

    }
}
