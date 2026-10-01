var BranchService = {};

BranchService.All = function () {
    return axios.get(appUrl + '/Maintenance/Branch?handler=All');
};
BranchService.AllMaintenance = function () {
    return axios.get(appUrl + '/Maintenance/Branch?handler=AllMaintenance');
};

BranchService.Search = function (params) {
    var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
    return axios.post(appUrl + '/Maintenance/Branch?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

BranchService.Delete = function (encryptedId) {
    return axios.put(appUrl + '/Maintenance/Branch?id=' + encryptedId + '&handler=Delete',
        JSON.stringify(encryptedId),
        { headers: headers });
};

BranchService.Restore = function (encryptedId) {
    return axios.put(appUrl + '/Maintenance/Branch?id=' + encryptedId + '&handler=Restore',
        JSON.stringify(encryptedId),
        { headers: headers });
};
BranchService.Save = function (data) {
    return axios.post(appUrl + '/Maintenance/Branch?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};
