using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Branch : Base
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(500)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string BankingHour { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Officer { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string ContactNo { get; set; } = string.Empty;
        [Required(AllowEmptyStrings = true)]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }
}
