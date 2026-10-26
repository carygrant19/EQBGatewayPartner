const { createApp, ref, computed, onMounted } = Vue;

const controller = createApp({
    setup() {
        const isConnected = ref(false);
        const selectedTrace = ref(null);
        const isTraceLoading = ref(false);
        const selectedException = ref(null);

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
            return isConnected.value ? 'Live SignalR Connected' : 'Connecting Telemetry...';
        });

        const recentExceptionsCount = computed(() => {
            return metrics.value.recentExceptions ? metrics.value.recentExceptions.length : 0;
        });

        const getAspireTraceUrl = (traceId) => {
            if (!traceId) return aspireDashboardBase;
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

        // Kinakalkula ang visual bar percentage ng bawat span batay sa kabuuang tagal ng trace
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

        // Open Distributed Trace Inspection Modal
        const OpenTraceModal = async (traceId) => {
            if (!traceId) return;

            selectedTrace.value = null;
            isTraceLoading.value = true;
            $('#traceModal').modal('show');

            try {
                const response = await axios.get('/Home?handler=TraceDetails&traceId=' + encodeURIComponent(traceId));
                if (response && response.data) {
                    selectedTrace.value = response.data;
                }
            } catch (err) {
                console.error("Failed to load trace timeline from SQL database:", err);
            } finally {
                isTraceLoading.value = false;
            }
        };

        // Open System Exception Details Modal
        const OpenExceptionModal = (ex) => {
            if (!ex) return;
            selectedException.value = ex;
            $('#exceptionModal').modal('show');
        };

        const CopyStackTrace = () => {
            if (!selectedException.value || !selectedException.value.stackTrace) return;
            navigator.clipboard.writeText(selectedException.value.stackTrace).then(() => {
                if (typeof swal !== 'undefined') {
                    swal.fire({
                        toast: true,
                        position: 'top-end',
                        icon: 'success',
                        title: 'Stack trace copied to clipboard',
                        showConfirmButton: false,
                        timer: 2000
                    });
                }
            });
        };

        const LoadInitialMetrics = async () => {
            try {
                const response = await axios.get('/Home?handler=Metrics');
                if (response && response.data) {
                    metrics.value = response.data;
                }
            } catch (err) {
                console.warn("Initial dashboard fetch failed, waiting for SignalR...", err);
            }
        };

        const InitSignalR = () => {
            if (typeof signalR === 'undefined') {
                console.error("SignalR library not found. Verify signalr.min.js is in _Layout.cshtml.");
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

        onMounted(() => {
            LoadInitialMetrics();
            InitSignalR();
        });

        return {
            aspireDashboardBase,
            metrics,
            isConnected,
            connectionStatusBadge,
            connectionStatusText,
            recentExceptionsCount,
            getStatusBadge,
            getBarHeight,
            getWaterfallWidth,
            getAspireTraceUrl,
            formatJson,
            // Trace Modal
            selectedTrace,
            isTraceLoading,
            OpenTraceModal,
            // Exception Modal
            selectedException,
            OpenExceptionModal,
            CopyStackTrace
        };
    }
});

global.registerFormatPlugins(controller);
controller.mount('#controller');