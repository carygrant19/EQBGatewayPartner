using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("UserRole")]
    public class UserRole
    {
        [Key]
        public long Id { get; set; } = 0;
        public int UserId { get; set; } = 0;
        public int RoleId { get; set; } = 0;

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

}
