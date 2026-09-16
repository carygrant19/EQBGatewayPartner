using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Module : Base
    {
        [Required(AllowEmptyStrings = true)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ModuleType { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        [Required]
        public string ParentId { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(300)]
        public string Url { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Icon { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(200)]
        public string Permission { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string AuditContent { get; set; } = string.Empty;

        public bool Show { get; set; } = false;
    }
}
