using System.ComponentModel.DataAnnotations;

namespace Notification.API.DTOs.Request
{
    public class Mail
    {
        [Required]
        public string Subject { get; set; } = string.Empty;
        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public string To { get; set; } = string.Empty;
        public string? Cc { get; set; } = null;
        public string? Bcc { get; set; } = null;
        [Required]
        public bool IsHTML { get; set; } = false;
        
        public List<(byte[] File, string Name)>? Attachments = null;
    }
}
