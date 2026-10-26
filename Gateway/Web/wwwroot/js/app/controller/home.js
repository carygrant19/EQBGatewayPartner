const { createApp, reactive, ref, computed, onMounted } = Vue;

const controller = createApp({
    setup() {
        // ==========================================
        // 1. SIGNALR & TELEMETRY DASHBOARD STATE
        // ==========================================
        const isConnected = ref(false);
        const container = document.getElementById('controller');
        const rawAspireBase = container ? container.getAttribute('data-aspire-base') : 'http://localhost:18888';
        const aspireDashboardBase = (rawAspireBase || 'http://localhost:18888').replace(/\/$/, '');

        const metrics = ref({
            activeRoutesCount: 0,
            activeClientsCount: 0,
            unhealthyHostsCount: 0,
            totalRequestsToday: 0,
            status2xxCount: 0,
            status4xxCount: 0,
            status5xxCount: 0,
            successRatePercentage: 100,
            averageLatencyMs: 0,
            hourlyTraffic: [],
            topRoutes: [],
            recentLogs: [],
            recentExceptions: []
        });

        const connectionStatusBadge = computed(() => {
            return isConnected.value
                ? 'bg-success-subtle text-success border border-success-subtle'
                : 'bg-warning-subtle text-warning border border-warning-subtle';
        });

        const connectionStatusText = computed(() => {
            return isConnected.value ? 'SignalR Live' : 'Connecting...';
        });

        const recentExceptionsCount = computed(() => {
            return metrics.value.recentExceptions ? metrics.value.recentExceptions.length : 0;
        });

        const getAspireTraceUrl = (traceId) => {
            if (!traceId) return '#';
            return `${aspireDashboardBase}/traces/${traceId}`;
        };

        const getStatusBadge = (code) => {
            const strCode = String(code || '');
            if (strCode.startsWith('2') || strCode === 'SUCCESS') {
                return 'bg-success-subtle text-success border border-success-subtle';
            }
            if (strCode.startsWith('4') || strCode === 'NOT_FOUND') {
                return 'bg-warning-subtle text-warning border border-warning-subtle';
            }
            return 'bg-danger-subtle text-danger border border-danger-subtle';
        };

        const getBarHeight = (requests) => {
            if (!metrics.value.hourlyTraffic || metrics.value.hourlyTraffic.length === 0) return 5;
            const max = Math.max(...metrics.value.hourlyTraffic.map(h => h.requests || 0), 1);
            const calculated = Math.round((requests / max) * 100);
            return Math.max(calculated, 8);
        };

        // Diretsong tawag sa Razor Page handler (/Home?handler=Metrics)
        const LoadInitialMetrics = async () => {
            try {
                const response = await axios.get('/Home?handler=Metrics');
                if (response && response.data) {
                    metrics.value = response.data;
                }
            } catch (err) {
                console.warn("Initial HTTP fetch fallback, awaiting SignalR connection...", err);
            }
        };

        const InitSignalR = () => {
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not found. Ensure signalr.min.js is in _Layout.cshtml.");
                return;
            }

            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/dashboardHub")
                .withAutomaticReconnect([0, 2000, 5000, 10000])
                .build();

            connection.on("ReceiveMetrics", (payload) => {
                if (payload) {
                    metrics.value = payload;
                }
            });

            connection.onreconnecting(() => {
                isConnected.value = false;
            });

            connection.onreconnected(() => {
                isConnected.value = true;
                connection.invoke("JoinDashboard");
            });

            connection.start().then(async () => {
                isConnected.value = true;
                await connection.invoke("JoinDashboard");

                try {
                    const hubData = await connection.invoke("GetInitialMetrics");
                    if (hubData) {
                        metrics.value = hubData;
                    }
                } catch {
                }
            }).catch((err) => {
                console.error("SignalR Connection Error: ", err);
                isConnected.value = false;
            });
        };

        // ==========================================
        // 2. USER PERSONAL ACTIVITY LOGS & TABLE STATE
        // ==========================================
        const records = ref([]);
        const show_table = ref(false);
        const pageSizeArray = [5, 10, 20, 50];
        const params = reactive({
            pageNum: 1,
            pageSize: 5,
            sortColumn: "LogDate",
            descending: true,
            filters: []
        });

        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await UserService.UserActivityLog(params);

                if (result && result.data && result.data.totalRecord > 0) {
                    records.value = result.data.data;
                    show_table.value = true;

                    if (params.pageNum > result.data.totalPage) {
                        params.pageNum = params.pageNum - 1;
                        initPages(result.data.totalPage);
                        GetRecords();
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
            } catch (err) {
                console.error("Failed to load activity log:", err);
            } finally {
                $('.preloader').fadeOut('slow');
            }
        };

        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            GetRecords();
        };

        const initPages = (tp) => {
            if ($('#sync-pagination').data("twbs-pagination")) {
                $('#sync-pagination').twbsPagination('destroy');
            }
            $('#sync-pagination').twbsPagination({
                totalPages: tp,
                initiateStartPageClick: false,
                hideOnlyOnePage: true,
                startPage: params.pageNum,
                onPageClick: (evt, page) => {
                    params.pageNum = page;
                    GetRecords();
                }
            });
        };

        const ItemCountChange = () => {
            params.pageNum = 1;
            Search();
        };

        const Sort = (col) => {
            params.sortColumn = col;
            params.descending = !params.descending;
            GetRecords();
        };

        const SortClass = (col) => {
            if (params.sortColumn === col) {
                return params.descending ? 'fa-sort-down' : 'fa-sort-up';
            }
            return 'fa fa-sort';
        };

        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
            LoadInitialMetrics();
            InitSignalR();
        });

        return {
            // Activity log props
            records,
            show_table,
            pageSizeArray,
            params,
            ItemCountChange,
            Sort,
            SortClass,
            Search,
            // Telemetry props
            metrics,
            isConnected,
            connectionStatusBadge,
            connectionStatusText,
            recentExceptionsCount,
            getAspireTraceUrl,
            getStatusBadge,
            getBarHeight
        };
    }
});

global.registerFormatPlugins(controller);
controller.mount('#controller');