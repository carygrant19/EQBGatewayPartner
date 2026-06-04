using System.Text.Json.Serialization;

namespace Notification.API.DTOs.Response
{
    public class Mail
    {
        public string Status { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; } = string.Empty;
    }
}
