using Gateway.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gateway.BLL.DTO.Request
{
    public class RouteCategory : Base
    {
        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

    }
}