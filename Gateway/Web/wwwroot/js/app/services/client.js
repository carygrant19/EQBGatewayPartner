var ClientService = {};

ClientService.All = function () {
    return axios.get(appUrl + '/Application/Client?handler=All');
};

ClientService.Search = function (params) {
    var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
    return axios.post(appUrl + '/Application/Client?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

ClientService.Delete = function (encryptedId) {
    return axios.put(appUrl + '/Application/Client?id=' + encryptedId + '&handler=Delete',
        JSON.stringify(encryptedId),
        { headers: headers });
};

ClientService.Restore = function (encryptedId) {
    return axios.put(appUrl + '/Application/Client?id=' + encryptedId + '&handler=Restore',
        JSON.stringify(encryptedId),
        { headers: headers });
};
ClientService.Save = function (data) {
    return axios.post(appUrl + '/Application/Client?handler=Save',
        JSON.stringify(data),
        { headers: headers });
};
