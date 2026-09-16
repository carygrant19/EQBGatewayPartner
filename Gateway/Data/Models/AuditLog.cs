using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Log_Audit")]
    public class AuditLog
    {      
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty; 
        public string Terminal { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public int? ChangeBy { get; set; } = 0;
        public DateTime ActionDate { get; set; }
        public string OriginalData { get; set; } = string.Empty;
        public string NewData { get; set; } = string.Empty;

        [NotMapped]
        public string? Fullname { get; set; } = string.Empty;
        [ForeignKey("ChangeBy")]
        public User? User { get; set; }
    }
}
