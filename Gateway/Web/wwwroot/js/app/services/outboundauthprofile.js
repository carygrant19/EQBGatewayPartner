var OutboundAuthProfileService = {};

OutboundAuthProfileService.All = function () {
    return axios.get(appUrl + '/Maintenance/OutboundAuthProfile?handler=All');
};

OutboundAuthProfileService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Maintenance/OutboundAuthProfile?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

OutboundAuthProfileService.Save = function (data) {
    return axios.post(appUrl + '/Maintenance/OutboundAuthProfile?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};

OutboundAuthProfileService.Delete = function (data) {
    return axios.put(appUrl + '/Maintenance/OutboundAuthProfile?handler=Delete',
        JSON.stringify(data),
        { headers: headers });
};

OutboundAuthProfileService.Restore = function (data) {
    return axios.put(appUrl + '/Maintenance/OutboundAuthProfile?handler=Restore',
        JSON.stringify(data),
        { headers: headers });
};