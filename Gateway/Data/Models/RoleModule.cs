using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    [Table("RoleModulePermission")]
    public class RoleModulePermission
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; } = 0;
        public int RoleId { get; set; } = 0;
        public int ModuleId { get; set; } = 0;
        public int PermissionId { get; set; } = 0;

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [ForeignKey("ModuleId")]
        public Module? Module { get; set; }

        [ForeignKey("PermissionId")]
        public Permission? Permissions { get; set; }


    }
}
