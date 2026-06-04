using System.ComponentModel.DataAnnotations;

namespace HRAgent.Api.DTOs.Assistant.Request
{
    public class Chat
    {
        [Required]
        public required string SessionId { get; set; }
        [Required]
        public required string Message { get; set; }
        [Required]
        public required string EmployeeRank { get; set; }
    }
}
