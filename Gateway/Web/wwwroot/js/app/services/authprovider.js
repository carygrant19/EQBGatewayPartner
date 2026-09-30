var AuthProviderService = {};

AuthProviderService.All = function () {
    return axios.get(appUrl + '/Maintenance/AuthProvider?handler=All');
};

AuthProviderService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Maintenance/AuthProvider?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

AuthProviderService.Save = function (data) {
    return axios.post(appUrl + '/Maintenance/AuthProvider?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};

AuthProviderService.Delete = function (data) {
    return axios.put(appUrl + '/Maintenance/AuthProvider?handler=Delete',
        JSON.stringify(data),
        { headers: headers });
};

AuthProviderService.Restore = function (data) {
    return axios.put(appUrl + '/Maintenance/AuthProvider?handler=Restore',
        JSON.stringify(data),
        { headers: headers });
};