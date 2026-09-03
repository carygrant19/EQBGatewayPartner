var ApiEndpointService = {};

ApiEndpointService.Categories = function () {
    return axios.get(appUrl + '/ApiManagement/Index?handler=Categories');
};

ApiEndpointService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/ApiManagement/Index?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

ApiEndpointService.Save = function (data) {
    return axios.post(appUrl + '/ApiManagement/Index?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};

ApiEndpointService.Delete = function (id) {
    return axios.put(appUrl + '/ApiManagement/Index?id=' + id + '&handler=Delete',
        JSON.stringify(id),
        { headers: headers });
};

ApiEndpointService.Restore = function (id) {
    return axios.put(appUrl + '/ApiManagement/Index?id=' + id + '&handler=Restore',
        JSON.stringify(id),
        { headers: headers });
};