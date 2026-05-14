using System.ComponentModel.DataAnnotations;

namespace Instapay.Api.DTOs.Request
{
    public class Transaction
    {
        [Required]
        public string TraceNo { get; set; } = string.Empty;
        [Required]
        public string SenderName { get; set; } = string.Empty;
        [Required]
        public string SenderAccountNo { get; set; } = string.Empty;
        [Required]
        public string SenderAccountType { get; set; } = string.Empty;
        [Required]
        public string SenderBirthDate { get; set; } = string.Empty;
        [Required]
        public string SenderBirthPlace { get; set; } = string.Empty;
        [Required]
        public string SenderBirthCountry { get; set; } = string.Empty;
        [Required]
        public string ReceiverName { get; set; } = string.Empty;
        [Required]
        public string ReceiverBankCode { get; set; } = string.Empty;
        [Required]
        public string ReceiverAccountNo { get; set; } = string.Empty;
        [Required]
        public string ReceiverMobileNo { get; set; } = string.Empty;
        [Required]
        public string Amount { get; set; } = string.Empty;

    }
}
