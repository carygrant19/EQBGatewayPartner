using System.Text.Json.Serialization;

namespace Temenos.API.DTOs.Response.v1
{
    public class AccountDetails
    {
        public string AccountNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BirthDate { get; set; } 
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BirthPlace { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InCorpDate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InCorpPlace { get; set; }
        public string Nationality { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Sms { get; set; } = string.Empty;
    }
}
