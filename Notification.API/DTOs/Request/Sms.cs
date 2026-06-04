using System.ComponentModel.DataAnnotations;

namespace Notification.API.DTOs.Request
{
    public class Sms
    {
        [Required(ErrorMessage = "TextMessage field is required.")]
        [StringLength(1530, ErrorMessage = "TextMessage cannot exceed 1530 characters.")]
        public string TextMessage { get; set; } = string.Empty;

        [Required(ErrorMessage = "MobileNumber field is required.")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "TextMessage must be between 10 and 13 characters..")]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
