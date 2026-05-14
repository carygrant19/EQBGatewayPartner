namespace Common.DTOs.Response
{
    public class Authenticate
    {
        public string? Token { get; set; } = string.Empty;
        public string? Status { get; set; } = string.Empty;
        public string? Message { get; set; } = string.Empty;
    }
}
