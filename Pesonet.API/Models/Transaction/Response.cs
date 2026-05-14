namespace Pesonet.API.Models.Transaction
{
    internal class Response
    {
        public outward_message outward_message { get; set; } = new();
        public error_message error { get; set; } = new();
    }
    internal class outward_message
    {
        public int seq { get; set; }
        public int transaction_count { get; set; }
        public string currency { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string received_date { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    internal class error_message
    {
        public string message { get; set; } = string.Empty;
    }
}
