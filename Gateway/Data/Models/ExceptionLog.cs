using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Log_Exception")]
    public class ExceptionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? ModuleName { get; set; } = string.Empty;
        public string? Message { get; set; } = string.Empty;
        public string? Source { get; set; } = string.Empty;
        public string? StackTrace { get; set; } = string.Empty;
        public string? InnerException { get; set; } = string.Empty;
        public DateTime LogDate { get; set; } = DateTime.Now;
    }
}
