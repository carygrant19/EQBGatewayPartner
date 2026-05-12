using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("LogActivity")]
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public int UserId { get; set; } = default!;
        public string ModuleName { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string Details { get; set; } = default!;
        public DateTime LogDate { get; set; } = DateTime.Now;
    }
}
