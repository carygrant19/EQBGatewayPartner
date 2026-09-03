using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Client : Base
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Id { get; set; } = "0";

        [Required(AllowEmptyStrings = false)]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CompanyId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public bool SSLRequired { get; set; } = false;
    }
}