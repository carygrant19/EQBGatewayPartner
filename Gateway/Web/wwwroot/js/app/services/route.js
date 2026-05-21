var RouteService = {};

RouteService.All = function () {
    return axios.get(appUrl + '/Application/Routemanagement/Index?handler=All');
};

RouteService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Application/Routemanagement/Index?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

RouteService.Delete = function (id) {
    return axios.put(appUrl + '/Application/Routemanagement/Index?id=' + id + '&handler=Delete',
        JSON.stringify(id),
        { headers: headers });
};

RouteService.Restore = function (id) {
    return axios.put(appUrl + '/Application/Routemanagement/Index?id=' + id + '&handler=Restore',
        JSON.stringify(id),
        { headers: headers });
};

RouteService.Save = function (data) {
    return axios.post(appUrl + '/Application/Routemanagement/Index?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};