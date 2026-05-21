    var PermissionService = {};

    PermissionService.All = function () {
        return axios.get(appUrl + '/Security/Permission?handler=All');
    };

    PermissionService.Search = function (params) {
        var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
        return axios.post(appUrl + '/Security/Permission?handler=Filter',
            JSON.stringify(searchOption),
            { headers: headers });
    };

    PermissionService.Delete = function (encryptedId) {
        return axios.put(appUrl + '/Security/Permission?id=' + encryptedId + '&handler=Delete',
            JSON.stringify(encryptedId),
            { headers: headers });
    };

    PermissionService.Restore = function (encryptedId) {
        return axios.put(appUrl + '/Security/Permission?id=' + encryptedId + '&handler=Restore',
            JSON.stringify(encryptedId),
            { headers: headers });
    };
    PermissionService.Save = function (data) {
        return axios.post(appUrl + '/Security/Permission?handler=Save',
            JSON.stringify(data),
            { headers: headers });
    };
