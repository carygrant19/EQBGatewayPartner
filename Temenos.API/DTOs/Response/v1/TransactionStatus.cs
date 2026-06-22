namespace Temenos.API.DTOs.Response.v1
{
    public class TransactionStatus
    {
        public string UID { get; set; } = string.Empty;
        public string MessageKey { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string DateTimeReceived { get; set; } = string.Empty;
        public string DateTimeProcess { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        //public string MessageIn { get; set; } = string.Empty;
        //public string MessageOut { get; set; } = string.Empty;
    }
}
