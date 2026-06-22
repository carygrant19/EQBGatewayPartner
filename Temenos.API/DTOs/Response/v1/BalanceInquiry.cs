namespace Temenos.API.DTOs.Response.v1
{
    public class BalanceInquiry
    {
        public string AccountNo { get; set; } = string.Empty;
        public string Balance { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
    }
}
