using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    public class Base
    {
        [NotMapped]
        public string? CreatedBy { get; set; } = default!;
        [NotMapped]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        [NotMapped]
        public string? UpdatedBy { get; set; } = default!;
        [NotMapped]
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
        //public bool Deleted { get; set; } = false;
    }
}
