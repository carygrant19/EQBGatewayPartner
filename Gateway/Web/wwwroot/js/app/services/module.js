    var ModuleService = {};

    ModuleService.ModuleGroup = () => {
        return axios.get(appUrl + '/Security/Module?handler=ModuleGroup');
    };

    ModuleService.AllModule = () => {
        return axios.get(appUrl + '/Security/Module?handler=All');
    };
    ModuleService.AllParent = () => {
        return axios.get(appUrl + '/Security/Module?handler=AllParent');
    };  
    ModuleService.Search = function (params) {
        var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
        return axios.post(appUrl + '/Security/Module?handler=Filter',
            JSON.stringify(searchOption),
            { headers: headers });
    }; 
    ModuleService.Delete = function (encryptedId) {
        return axios.put(appUrl + '/Security/Module?id=' + encryptedId + '&handler=Delete',
            JSON.stringify(encryptedId),
            { headers: headers });
    };

    ModuleService.Restore = function (encryptedId) {
        return axios.put(appUrl + '/Security/Module?id=' + encryptedId + '&handler=Restore',
            JSON.stringify(encryptedId),
            { headers: headers });
    };
    ModuleService.Save = function (data) {
        return axios.post(appUrl + '/Security/Module?handler=Save',
            JSON.stringify(data),
            { headers: headers });
    };
