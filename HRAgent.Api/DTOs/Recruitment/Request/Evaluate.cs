using System.ComponentModel.DataAnnotations;

namespace HRAgent.Api.DTOs.Recruitment.Request
{
    public class Evaluate
    {
        [Required]
        public string JobType { get; set; } = string.Empty;
        [Required]
        public string JobDescription { get; set; } = string.Empty;
        [Required]
        public string Profile { get; set; } = string.Empty;
    }
}
