var ReportService = {};

ReportService.Routes = function () {
    return axios.get(appUrl + '/Reports/TransactionLogs?handler=Routes');
};

ReportService.Clients = function () {
    return axios.get(appUrl + '/Reports/TransactionLogs?handler=Clients');
};

ReportService.Search = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Reports/TransactionLogs?handler=Filter',
        JSON.stringify(searchOption),
        { headers: headers });
};

ReportService.ExportCsv = function (params) {
    var searchOption = {
        PageNum: params.pageNum,
        PageSize: params.pageSize,
        Filters: params.filters,
        SortColumn: params.sortColumn,
        Descending: params.descending
    };
    return axios.post(appUrl + '/Reports/TransactionLogs?handler=ExportCsv',
        JSON.stringify(searchOption),
        { headers: headers, responseType: 'blob' });
};

ReportService.TraceDetails = function (traceId) {
    return axios.get(appUrl + '/Reports/TransactionLogs?handler=TraceDetails&traceId=' + encodeURIComponent(traceId));
};