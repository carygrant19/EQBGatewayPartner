using System.ComponentModel.DataAnnotations;

namespace Temenos.API.DTOs.Request
{
    public class Transaction
    {
        [Required]
        public string TransactionType { get; set; } = string.Empty;
        public string DebitAmount { get; set; } = string.Empty;
        public string DebitValueDate { get; set; } = string.Empty;
        public string DebitAccountNumber { get; set; } = string.Empty;
        public string DebitCurrency { get; set; } = "PHP";
        public string CreditValueDate { get; set; } = string.Empty;
        public string CreditAccountNumber { get; set; } = string.Empty;
        public string CreditCurrency { get; set; } = "PHP";
        public string OrderingCust { get; set; } = "PAYMENT";
        public string OrderingBank { get; set; } = "EQB";
        public string ReferenceNo {  get; set; } = string.Empty;
    }
}
