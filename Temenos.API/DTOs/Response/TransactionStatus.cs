namespace Temenos.API.DTOs.Response
{
    public class TransactionStatus
    {
        public string UID { get; set; } = string.Empty;
        public string MESSAGE_KEY { get; set; } = string.Empty;
        public string TRANS_REFERENCE { get; set; } = string.Empty;
        public string DATE_TIME_RECEIVED { get; set; } = string.Empty;
        public string DATE_TIME_PROCESS { get; set; } = string.Empty;
        public string STATUS { get; set; } = string.Empty;
        public string MSG_IN { get; set; } = string.Empty;
        public string MSG_OUT { get; set; } = string.Empty;
    }
}
