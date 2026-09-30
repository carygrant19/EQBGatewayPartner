var ClientService = {};

ClientService.All = function () {
    return axios.get(appUrl + '/Maintenance/Client?handler=All');
};

ClientService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Maintenance/Client?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

ClientService.Save = function (data) {
    return axios.post(appUrl + '/Maintenance/Client?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};

ClientService.Delete = function (data) {
    return axios.put(appUrl + '/Maintenance/Client?handler=Delete',
        JSON.stringify(data),
        { headers: headers });
};

ClientService.Restore = function (data) {
    return axios.put(appUrl + '/Maintenance/Client?handler=Restore',
        JSON.stringify(data),
        { headers: headers });
};

ClientService.ResetCredentials = function (data) {
    return axios.post(appUrl + '/Maintenance/Client?handler=ResetCredentials',
        JSON.stringify(data),
        { headers: headers });
};