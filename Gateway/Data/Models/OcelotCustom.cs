
using Ocelot.Configuration.File;
namespace Gateway.Data.Models
{
    public class OcelotCustomFileConfiguration : FileConfiguration
    { 
        public new CustomFileGlobalConfiguration GlobalConfiguration { get; set; } = new(); 
        public new List<CustomFileRoute> Routes { get; set; } = [];
    }

    public class CustomFileGlobalConfiguration : FileGlobalConfiguration
    { 
        public string? ClientIdHeader { get; set; }
        public string? QuotaExceededMessage { get; set; }
        public int RateLimitHttpStatusCode { get; set; }
        public bool EnableRateLimitHeaders { get; set; }
        public string? LogLevel { get; set; }
        public bool EnableRequestId { get; set; }
    }

    public class CustomFileRoute : FileRoute
    { 
        public string? Id { get; set; }
        public bool RequireSignature { get; set; } 
        public TimeLimit? TimeLimit { get; set; } 
        public List<string> Client { get; set; } = []; 
    }

    public class TimeLimit
    {
        public bool EnableTimeLimit { get; set; }
        public string? TimeFrom { get; set; }
        public string? TimeTo { get; set; }
        public List<int> AllowedDays { get; set; } = [];
    }
}
