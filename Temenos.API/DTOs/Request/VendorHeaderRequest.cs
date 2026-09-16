using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Temenos.API.DTOs.Request
{
    public class VendorHeaderRequest
    {
        [FromHeader(Name = "X-Vendor-Username")]
        public string VendorUsername { get; set; } = string.Empty;

        [FromHeader(Name = "X-Vendor-Password")]
        public string VendorPassword { get; set; } = string.Empty;
        [FromHeader(Name = "X-Vendor-Api-Key")]
        public string VendorApiKey { get; set; } = string.Empty;

        [FromHeader(Name = "X-Vendor-Api-Secret")]
        public string VendorApiSecret { get; set; } = string.Empty;
    }
}
