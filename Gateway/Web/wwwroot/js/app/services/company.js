var CompanyService = {};

CompanyService.All = function () {
    return axios.get(appUrl + '/Maintenance/Company?handler=All');
};

CompanyService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Maintenance/Company?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

CompanyService.Save = function (data) {
    return axios.post(appUrl + '/Maintenance/Company?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};

CompanyService.Delete = function (data) {
    return axios.put(appUrl + '/Maintenance/Company?handler=Delete',
        JSON.stringify(data),
        { headers: headers });
};

CompanyService.Restore = function (data) {
    return axios.put(appUrl + '/Maintenance/Company?handler=Restore',
        JSON.stringify(data),
        { headers: headers });
};