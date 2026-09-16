using Gateway.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class FParam : Base
    {
        [Required]
        public int PageNum { get; set; } = 0;
        [Required]
        public int PageSize { get; set; } = 0;
        [Required]
        public string SortColumn { get; set; } = default!;
        [Required]
        public bool Descending { get; set; } = false!;

        public List<Filter> Filters { get; set; } = new List<Filter>();
    }
}
