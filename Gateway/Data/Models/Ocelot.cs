using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Gateway.Data.Models
{
    public class OcelotConfig
    {
        [JsonPropertyName("GlobalConfiguration")]
        public GlobalConfiguration GlobalConfiguration { get; set; } = new();

        [JsonPropertyName("Routes")]
        public List<ORoute> Routes { get; set; } = new();
    }

    public class GlobalConfiguration
    {
        [JsonPropertyName("BaseUrl")]
        public string? BaseUrl { get; set; }

        [JsonPropertyName("RequestSizeLimit")]
        public long? RequestSizeLimit { get; set; }
    }

    public class ORoute
    {
        [JsonPropertyName("_comment")]
        public string? _comment { get; set; }

        [JsonPropertyName("Id")]
        public string? Id { get; set; }

        [JsonPropertyName("DownstreamPathTemplate")]
        public string? DownstreamPathTemplate { get; set; }

        [JsonPropertyName("DownstreamScheme")]
        public string? DownstreamScheme { get; set; }

        [JsonPropertyName("DownstreamHostAndPorts")]
        public List<DownstreamHostAndPorts>? DownstreamHostAndPorts { get; set; }

        [JsonPropertyName("UpstreamPathTemplate")]
        public string? UpstreamPathTemplate { get; set; }

        [JsonPropertyName("UpstreamHttpMethod")]
        public List<string>? UpstreamHttpMethod { get; set; }

        [JsonPropertyName("AuthenticationOptions")]
        public OAuthenticationOptions? AuthenticationOptions { get; set; }

        [JsonPropertyName("RateLimitOptions")]
        public RateLimit? RateLimitOptions { get; set; }

        [JsonPropertyName("SecurityOptions")]
        public SecurityOptions? SecurityOptions { get; set; }

        [JsonPropertyName("Client")]
        public List<string>? Client { get; set; }

        [JsonPropertyName("TimeLimit")]
        public TimeLimit? TimeLimit { get; set; }

        [JsonPropertyName("RequireSignature")]
        public bool RequireSignature { get; set; }
    }

    public class DownstreamHostAndPorts
    {
        [JsonPropertyName("Host")]
        public string Host { get; set; } = string.Empty;

        [JsonPropertyName("Port")]
        public int Port { get; set; }
    }

    public class OAuthenticationOptions
    {
        [JsonPropertyName("AuthenticationProviderKey")]
        public string? AuthenticationProviderKey { get; set; }
    }

    public class RateLimit
    {
        [JsonPropertyName("EnableRateLimiting")]
        public bool EnableRateLimiting { get; set; }

        [JsonPropertyName("Period")]
        public string? Period { get; set; }

        [JsonPropertyName("PeriodTimespan")]
        public int PeriodTimespan { get; set; }

        [JsonPropertyName("Limit")]
        public int Limit { get; set; }
    }

    public class SecurityOptions
    {
        [JsonPropertyName("ExcludeAllowedFromBlocked")]
        public bool ExcludeAllowedFromBlocked { get; set; }

        [JsonPropertyName("IPAllowedList")]
        public List<string>? IPAllowedList { get; set; }

        [JsonPropertyName("IPBlockedList")]
        public List<string>? IPBlockedList { get; set; }
    }
     
}
