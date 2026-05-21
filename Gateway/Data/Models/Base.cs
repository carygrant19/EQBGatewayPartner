using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    public class Base
    {
        [NotMapped]
        public int? CreatedBy { get; set; } = default!;
        [NotMapped]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        [NotMapped]
        public int? UpdatedBy { get; set; } = default!;
        [NotMapped]
        public DateTime? UpdatedDate { get; set; } = DateTime.Now; 
    }
}
