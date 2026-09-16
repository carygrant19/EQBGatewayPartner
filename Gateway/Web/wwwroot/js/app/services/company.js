    var CompanyService = {};

    CompanyService.All = function () {
        return axios.get(appUrl + '/Application/Company?handler=All');
    };

    CompanyService.Search = function (params) {
        var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
        return axios.post(appUrl + '/Application/Company?handler=Filter',
            JSON.stringify(searchOption),
            { headers: headers });
    };

    CompanyService.Delete = function (encryptedId) {
        return axios.put(appUrl + '/Application/Company?id=' + encryptedId + '&handler=Delete',
            JSON.stringify(encryptedId),
            { headers: headers });
    };

    CompanyService.Restore = function (encryptedId) {
        return axios.put(appUrl + '/Application/Company?id=' + encryptedId + '&handler=Restore',
            JSON.stringify(encryptedId),
            { headers: headers });
    };
    CompanyService.Save = function (data) {
        return axios.post(appUrl + '/Application/Company?handler=Save',
            JSON.stringify(data),
            { headers: headers });
    };
