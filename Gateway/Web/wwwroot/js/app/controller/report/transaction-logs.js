const { createApp, reactive, ref, onMounted } = Vue;

const controller = createApp({
    setup() {
        const routes = ref([]);
        const clients = ref([]);
        const totalRecord = ref(0);
        const selectedTrace = ref(null);
        const isTraceLoading = ref(false);

        const summary = reactive({
            totalSuccess2xx: 0,
            totalError4xx5xx: 0,
            averageLatencyMs: 0
        });

        const getTodayRange = () => {
            const now = new Date();
            const pad = (n) => String(n).padStart(2, '0');
            const dateStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
            return {
                start: `${dateStr}T00:00`,
                end: `${dateStr}T23:59`
            };
        };

        const defaults = getTodayRange();

        const search = reactive({
            startDate: defaults.start,
            endDate: defaults.end,
            clientId: '',
            routeId: '',
            statusCategory: 'ALL',
            transactionId: '',
            traceOrUri: ''
        });

        params.sortColumn = "RequestDate";
        params.descending = true;

        onMounted(() => {
            global.GetRecords = GetRecords;
            LoadRoutes();
            LoadClients();
            Search();
        });

        const LoadRoutes = async () => {
            const result = await ReportService.Routes();
            if (result && result.data && Array.isArray(result.data)) {
                routes.value = result.data;
            }
        };

        const LoadClients = async () => {
            const result = await ReportService.Clients();
            if (result && result.data && Array.isArray(result.data)) {
                clients.value = result.data;
            }
        };

        const GetRecords = async () => {
            $(".preloader").show();

            const result = await ReportService.Search(params);

            if (result && result.data) {
                if (result.data.status === 'ERROR') {
                    swal.fire({
                        icon: 'error',
                        title: 'Oops...',
                        text: result.data.message
                    });
                    $('.preloader').fadeOut('slow');
                    return;
                }

                totalRecord.value = result.data.totalRecord || 0;
                summary.totalSuccess2xx = result.data.totalSuccess2xx || 0;
                summary.totalError4xx5xx = result.data.totalError4xx5xx || 0;
                summary.averageLatencyMs = result.data.averageLatencyMs || 0;

                if (result.data.totalRecord > 0) {
                    records.value = result.data.data;
                    show_table.value = true;

                    if (params.pageNum > result.data.totalPage) {
                        params.pageNum = params.pageNum - 1;
                        initPages(result.data.totalPage);
                    }
                    else if (result.data.totalPage > params.pageNum) {
                        initPages(result.data.totalPage);
                    }
                    else if (result.data.totalPage > 1) {
                        initPages(result.data.totalPage);
                    }
                    else if ($('#sync-pagination').data("twbs-pagination")) {
                        $('#sync-pagination').twbsPagination('destroy');
                    }
                }
                else {
                    records.value = [];
                    show_table.value = false;
                    if ($('#sync-pagination').data("twbs-pagination")) {
                        $('#sync-pagination').twbsPagination('destroy');
                    }
                }
            }

            $('.preloader').fadeOut('slow');
        };

        const BuildFilters = () => {
            params.filters = [];

            if (search.startDate || search.endDate) {
                params.filters.push({
                    Property: "RequestDate",
                    Operator: "BETWEEN",
                    Value: search.startDate || null,
                    Value2: search.endDate || null,
                    Values: null,
                    IsDate: true
                });
            }

            if (search.clientId) {
                params.filters.push({
                    Property: "ClientId",
                    Operator: "EQUALS",
                    Value: search.clientId,
                    Value2: null,
                    Values: null,
                    IsDate: false
                });
            }

            if (search.routeId) {
                params.filters.push({
                    Property: "RouteId",
                    Operator: "EQUALS",
                    Value: search.routeId,
                    Value2: null,
                    Values: null,
                    IsDate: false
                });
            }

            if (search.statusCategory && search.statusCategory !== 'ALL') {
                params.filters.push({
                    Property: "StatusCategory",
                    Operator: "EQUALS",
                    Value: search.statusCategory,
                    Value2: null,
                    Values: null,
                    IsDate: false
                });
            }

            if (search.transactionId && search.transactionId.trim() !== '') {
                params.filters.push({
                    Property: "TransactionId",
                    Operator: "CONTAINS",
                    Value: search.transactionId.trim(),
                    Value2: null,
                    Values: null,
                    IsDate: false
                });
            }

            if (search.traceOrUri && search.traceOrUri.trim() !== '') {
                const term = search.traceOrUri.trim();
                params.filters.push({
                    Property: term.startsWith('/') ? "Uri" : "TraceId",
                    Operator: "CONTAINS",
                    Value: term,
                    Value2: null,
                    Values: null,
                    IsDate: false
                });
            }
        };

        const Search = () => {
            params.pageNum = 1;
            BuildFilters();
            GetRecords();
        };

        const ResetFilters = () => {
            const range = getTodayRange();
            search.startDate = range.start;
            search.endDate = range.end;
            search.clientId = '';
            search.routeId = '';
            search.statusCategory = 'ALL';
            search.transactionId = '';
            search.traceOrUri = '';
            Search();
        };

        const ExportCsv = async () => {
            $(".preloader").show();
            BuildFilters();

            const result = await ReportService.ExportCsv(params);
            if (result && result.data) {
                const blob = new Blob([result.data], { type: 'text/csv;charset=utf-8;' });
                const url = window.URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.setAttribute('download', `Gateway_Transaction_Report_${new Date().toISOString().slice(0, 19).replace(/[-T:]/g, '')}.csv`);
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);
            }

            $('.preloader').fadeOut('slow');
        };

        const OpenTraceModal = async (traceId) => {
            if (!traceId) return;
            selectedTrace.value = null;
            isTraceLoading.value = true;
            $('#traceModal').modal('show');

            const result = await ReportService.TraceDetails(traceId);
            if (result && result.data) {
                if (result.data.status === 'ERROR') {
                    $('#traceModal').modal('hide');
                    swal.fire({
                        icon: 'error',
                        title: 'Oops...',
                        text: result.data.message
                    });
                } else {
                    selectedTrace.value = result.data;
                }
            }
            isTraceLoading.value = false;
        };

        const CopyText = (text) => {
            if (!text) return;
            navigator.clipboard.writeText(text).then(() => {
                swal.fire({
                    toast: true,
                    position: 'top-end',
                    icon: 'success',
                    title: 'Copied to clipboard',
                    showConfirmButton: false,
                    timer: 1500
                });
            });
        };

        const getStatusBadge = (code) => {
            const strCode = String(code || '');
            if (strCode.startsWith('2') || strCode === 'SUCCESS') return 'bg-success-subtle text-success border border-success-subtle';
            if (strCode.startsWith('4')) return 'bg-warning-subtle text-warning border border-warning-subtle';
            return 'bg-danger-subtle text-danger border border-danger-subtle';
        };

        const getWaterfallWidth = (durationMs, totalDurationMs) => {
            if (!totalDurationMs || totalDurationMs <= 0) return 15;
            const percent = Math.round((durationMs / totalDurationMs) * 100);
            return Math.min(Math.max(percent, 8), 100);
        };

        const formatJson = (val) => {
            if (!val) return 'No Payload Recorded';
            try {
                const parsed = typeof val === 'object' ? val : JSON.parse(val);
                return JSON.stringify(parsed, null, 2);
            } catch {
                return val;
            }
        };

        const returnProps = {
            pageSizeArray,
            params,
            show_table,
            totalRecord,
            routes,
            clients,
            summary,
            search,
            records,
            selectedTrace,
            isTraceLoading
        };

        const returnMethod = {
            ItemCountChange: () => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,
            ResetFilters,
            ExportCsv,
            OpenTraceModal,
            CopyText,
            getStatusBadge,
            getWaterfallWidth,
            formatJson
        };

        return {
            ...returnProps,
            ...returnMethod
        };
    }
});

if (typeof global !== 'undefined' && global.registerFormatPlugins) {
    global.registerFormatPlugins(controller);
}
controller.mount('#controller');