using System;
using System.Collections.Generic;

namespace Gateway.BLL.DTO.Response
{
    public class VRouteCategory : ListBase
    {
        public List<FRouteCategory> Data { get; set; } = [];
    }

    public class FRouteCategory : RouteCategory
    {
        public List<RouteCategory> Category { get; set; } = []; 
    } 

    public class RouteCategory
    {
        public string Id { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}