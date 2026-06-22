using System.Text.Json.Serialization;

namespace Common.DTOs.Response.Api
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Meta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Error>? Errors { get; set; }
    }

    public class Error
    {
        public string? Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
