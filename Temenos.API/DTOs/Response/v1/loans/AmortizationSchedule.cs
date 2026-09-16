using System.Text.Json.Serialization;

namespace Temenos.API.DTOs.Response.v1.loans
{
    public class AmortizationSchedule
    {
        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }
        [JsonPropertyName("totalDue")]
        public string? TotalDue { get; set; }
        [JsonPropertyName("principal")]
        public string? Principal { get; set; }
        [JsonPropertyName("interest")]
        public string? Interest { get; set; }
        [JsonPropertyName("outstanding")]
        public string? Outstanding { get; set; }
    }

}
