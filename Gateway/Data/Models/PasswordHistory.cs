using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("Master_Password_History")]
    public class PasswordHistory
    {
        [Key]
        public long Id { get; set; } = 0;
        public int UserId { get; set; } = 0;
        public string Password { get; set; } = string.Empty;
        public DateTime ChangedDate { get; set; } = default;

    }
}
