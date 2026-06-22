using System.ComponentModel.DataAnnotations;

namespace Temenos.API.DTOs.Request.v1
{
    public class FundTransfer
    {
        [Required]
        public string TransactionType { get; set; } = string.Empty;
        [Required]
        public string Amount { get; set; } = string.Empty;
        [Required]
        public string DebitValueDate { get; set; } = string.Empty;
        [Required]
        public string DebitAccountNumber { get; set; } = string.Empty;
        public string DebitCurrency { get; set; } = "PHP";
        [Required]
        public string CreditValueDate { get; set; } = string.Empty;
        [Required]
        public string CreditAccountNumber { get; set; } = string.Empty;
        public string CreditCurrency { get; set; } = "PHP";
        //public string OrderingCust { get; set; } = "PAYMENT";
        //public string OrderingBank { get; set; } = "EQB";
        public string ReferenceNo {  get; set; } = string.Empty;
    }
}
