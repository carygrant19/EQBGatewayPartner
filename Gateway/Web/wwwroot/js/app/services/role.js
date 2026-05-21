
    var RoleService = {};

    RoleService.All = function () {
        return axios.get(appUrl + '/Security/Role?handler=All');
    };

    RoleService.ByUsername = (username) => {
        return axios.get(appUrl + '/Security/Role?username=' + username + '&handler=RoleModuleByUsername');
    };
    RoleService.ById = (roleId) => {
        return axios.get(appUrl + '/Security/Role?roleId=' + roleId + '&handler=ModuleByRoleId');
    };

    RoleService.Search = function (params) {
        var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
        return axios.post(appUrl + '/Security/Role?handler=Filter',
            JSON.stringify(searchOption),
            { headers: headers });
    };

    RoleService.Delete = function (encryptedId) {
        return axios.put(appUrl + '/Security/Role?id=' + encryptedId + '&handler=Delete',
            JSON.stringify(encryptedId),
            { headers: headers });
    };

    RoleService.Restore = function (encryptedId) {
        return axios.put(appUrl + '/Security/Role?id=' + encryptedId + '&handler=Restore',
            JSON.stringify(encryptedId),
            { headers: headers });
    };
    RoleService.Save = function (data) {
        return axios.post(appUrl + '/Security/Role?handler=Save',
            JSON.stringify(data),
            { headers: headers });
    };
     