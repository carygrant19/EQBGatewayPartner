using System.ComponentModel.DataAnnotations;

namespace Pesonet.API.DTOs.Request
{
    public class Transaction
    {
        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string SenderAccountNo { get; set; } = string.Empty;

        [Required]
        public string ReceiverName { get; set; } = string.Empty;
        [Required]
        public string ReceiverAddress { get; set; } = string.Empty;
        [Required]
        public string ReceiverAccountNo { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; } = 0;

        [Required]
        public string Bicfi { get; set; } = string.Empty;


    }
}
