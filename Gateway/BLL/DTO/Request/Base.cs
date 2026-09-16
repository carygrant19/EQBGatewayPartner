using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class Base
    {
        [Required]
        [MaxLength(20)]
        public string? OpUser { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string? OpUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string? Terminal { get; set; } = string.Empty;
    }
}
