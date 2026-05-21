var UserService = {};
var csrfToken = $('input:hidden[name="__RequestVerificationToken"]').val();
var contentType = 'application/json';
var headers = {
    "XSRF-TOKEN": csrfToken,
    "Content-Type": contentType
};
UserService.All = function () {
    return axios.get(appUrl + '/Security/User?handler=All');
};

UserService.Search = function (params) {
    var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
    return axios.post(appUrl + '/Security/User?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};
UserService.CheckAD = (username) => {
    return axios.post(appUrl + '/Security/User?handler=CheckAD',
        JSON.stringify(username),
        { headers: headers });
};
UserService.Delete = function (encryptedId) {
    return axios.put(appUrl + '/Security/User?id=' + encryptedId + '&handler=Delete',
        JSON.stringify(encryptedId),
        { headers: headers });
};

UserService.Restore = function (encryptedId) {
    return axios.put(appUrl + '/Security/User?id=' + encryptedId + '&handler=Restore',
        JSON.stringify(encryptedId),
        { headers: headers });
};
UserService.Save = function (data) {
    return axios.post(appUrl + '/Security/User?handler=Save',
        JSON.stringify(data),
        { headers: headers });
}; 

UserService.ResetPassword = (username) => {
    return axios.post(appUrl + '/Security/User?handler=ResetPassword',
        JSON.stringify(username),
        { headers: headers });
};
UserService.UnlockUser = (username) => {
    return axios.post(appUrl + '/Security/User?handler=UnlockUser',
        JSON.stringify(username),
        { headers: headers });
};

UserService.UserActivityLog = (params) => {
    var searchOption = { PageNum: params.pageNum, PageSize: params.pageSize, Filters: params.filters, SortColumn: params.sortColumn, Descending: params.descending };
    return axios.post(appUrl + '/Home?handler=FilterActivityLog',
        JSON.stringify(searchOption),
        { headers: headers });
};
