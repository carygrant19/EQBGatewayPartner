using System.ComponentModel.DataAnnotations.Schema;

namespace Gateway.Data.Models
{
    public class Base
    {
        public int? CreatedBy { get; set; } = default!;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; } = default!;
        public DateTime? UpdatedDate { get; set; } = DateTime.Now; 
    }
}
