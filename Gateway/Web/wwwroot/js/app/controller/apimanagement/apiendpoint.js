const { createApp, reactive, ref, computed, onMounted, nextTick } = Vue;

const controller = createApp({
    setup() {
        const records = ref([]);
        const categories = ref([]);
        const authProviders = ref([]);
        const outboundAuthProfiles = ref([]);
        const clients = ref([]); // <-- IDINAGDAG PARA SA INBOUND CLIENTS SELECTION
        const show_table = ref(false);
        const actionMode = ref("Add");
        const viewData = ref(null);

        // HTTP Methods State
        const availableMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
        const selectedMethods = ref([]);

        const tabErrors = reactive({
            basic: 0,
            security: 0,
            target: 0,
            transform: 0,
            resilience: 0,
            traffic: 0
        });

        const totalErrors = computed(() => {
            return tabErrors.basic + tabErrors.security + tabErrors.target + tabErrors.transform + tabErrors.resilience + tabErrors.traffic;
        });

        const parsedViewMethods = computed(() => {
            if (!viewData.value) return [];
            const source = viewData.value.allowedMethods || viewData.value.upstreamHttpMethod || "GET";
            return source.replace(/\|/g, ',').split(',').map(m => m.trim().toUpperCase()).filter(m => m);
        });

        const params = reactive({
            pageNum: 1,
            pageSize: 10,
            sortColumn: "Name",
            descending: false,
            filters: []
        });

        const search = reactive({ keyword: "" });
        const disableControl = reactive({ code: false });
        const selectedDays = ref([]);

        const formData = reactive({
            id: "0",
            code: "",
            name: "",
            description: "",
            categoryId: null,
            authProviderId: null,
            outboundAuthProfileId: null,
            clientIds: [], // <-- IDINAGDAG: Array para sa Multi-Select Client IDs
            isActive: true,
            isWebSocket: false,
            requireApiKey: true,
            enableCatchAll: false,
            upstreamBase: "",
            downstreamBase: "",
            upstreamPathTemplate: "",
            upstreamHttpMethod: "GET,POST",
            downstreamPathTemplate: "",
            downstreamScheme: "https",
            priority: 1,
            integrationType: "PROXY",
            stripPath: true,
            preserveHostHeader: false,
            allowedMethods: "GET,POST,PUT,DELETE",
            apiVersion: "v1",
            maxRetries: 0,
            retryDelayMs: 1000,
            enableCircuitBreaker: false,
            mockResponseCode: 200,
            mockResponseBody: '{\n  "message": "Mock Response Success"\n}',
            enableRateLimiting: false,
            rateLimit: 100,
            ratePeriodTimespan: 60,
            timeFrom: null,
            timeTo: null,
            allowedDays: "",
            loadBalancingPolicy: null,
            timeoutSeconds: 30,
            enableCaching: false,
            cacheTtlSeconds: 60,
            targetHosts: [],
            ipRules: [],
            transforms: []
        });

        onMounted(() => {
            global.GetRecords = GetRecords;
            GetCategories();
            GetAuthProviders();
            GetOutboundAuthProfiles();
            GetClients(); // <-- IDINAGDAG: Fetch Clients sa Initialization
            GetRecords();
        });

        const getMethodBadgeClass = (method) => {
            switch (method) {
                case 'GET': return 'bg-success';
                case 'POST': return 'bg-primary';
                case 'PUT': return 'bg-warning text-dark';
                case 'DELETE': return 'bg-danger';
                case 'PATCH': return 'bg-info text-dark';
                default: return 'bg-secondary';
            }
        };

        const CleanPath = (path) => {
            if (!path) return "";
            return path.replace(/\/\{\*\*(catch-all|remainder)\}/gi, '')
                .replace(/\{\*\*(catch-all|remainder)\}/gi, '')
                .replace(/\/$/, '');
        };

        const GetCategories = async () => {
            try {
                const response = await ApiEndpointService.Categories();
                if (response.data) categories.value = response.data;
            } catch (error) {
                console.error("Failed to load categories", error);
            }
        };

        const GetAuthProviders = async () => {
            try {
                if (typeof ApiEndpointService.AuthProviders === 'function') {
                    const response = await ApiEndpointService.AuthProviders();
                    if (response.data) authProviders.value = response.data;
                }
            } catch (error) {
                console.error("Failed to load auth providers", error);
            }
        };

        const GetOutboundAuthProfiles = async () => {
            try {
                if (typeof ApiEndpointService.OutboundAuthProfiles === 'function') {
                    const response = await ApiEndpointService.OutboundAuthProfiles();
                    if (response.data) outboundAuthProfiles.value = response.data;
                }
            } catch (error) {
                console.error("Failed to load outbound auth profiles", error);
            }
        };

        // IDINAGDAG: Fetch Active Inbound Clients for Dropdown
        const GetClients = async () => {
            try {
                if (typeof ApiEndpointService.Clients === 'function') {
                    const response = await ApiEndpointService.Clients();
                    if (response.data) clients.value = response.data;
                }
            } catch (error) {
                console.error("Failed to load clients", error);
            }
        };

        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await ApiEndpointService.Search(params);
                if (result.data && result.data.totalRecord > 0) {
                    records.value = result.data.data;
                    show_table.value = true;

                    if (params.pageNum > result.data.totalPage) {
                        params.pageNum = params.pageNum - 1;
                        initPages(result.data.totalPage);
                    } else if (result.data.totalPage > 1) {
                        initPages(result.data.totalPage);
                    }
                } else {
                    records.value = [];
                    show_table.value = false;
                    if ($('#sync-pagination').data("twbs-pagination")) {
                        $('#sync-pagination').twbsPagination('destroy');
                    }
                }
            } catch (error) {
                console.error(error);
            } finally {
                $('.preloader').fadeOut('slow');
            }
        };

        const ValidateTabs = () => {
            $('#dataForm').parsley().validate();

            const countErrorsInTab = (tabSelector) => {
                let errorCount = 0;
                $(tabSelector).find('input, select, textarea').each(function () {
                    const fieldInstance = $(this).parsley();
                    if (fieldInstance && fieldInstance.isValid() === false) {
                        errorCount++;
                    }
                });
                return errorCount;
            };

            tabErrors.basic = countErrorsInTab('#v-basic');
            if (selectedMethods.value.length === 0) {
                tabErrors.basic++;
            }

            tabErrors.security = countErrorsInTab('#v-security');
            tabErrors.target = formData.integrationType === 'PROXY' ? countErrorsInTab('#v-target') : 0;
            tabErrors.transform = formData.integrationType === 'PROXY' ? countErrorsInTab('#v-transform') : 0;
            tabErrors.resilience = countErrorsInTab('#v-resilience');
            tabErrors.traffic = countErrorsInTab('#v-traffic');

            return totalErrors.value === 0;
        };

        const ResetTabErrors = () => {
            tabErrors.basic = 0;
            tabErrors.security = 0;
            tabErrors.target = 0;
            tabErrors.transform = 0;
            tabErrors.resilience = 0;
            tabErrors.traffic = 0;
        };

        const Add = () => {
            if ($('#dataForm').parsley()) $('#dataForm').parsley().reset();
            actionMode.value = "Add";
            disableControl.code = false;
            ResetTabErrors();

            formData.id = "0";
            formData.code = "";
            formData.name = "";
            formData.description = "";
            formData.categoryId = categories.value.length > 0 ? categories.value[0].id : null;
            formData.authProviderId = null;
            formData.outboundAuthProfileId = null;
            formData.clientIds = []; // <-- RESET SELECTED CLIENTS
            formData.isActive = true;
            formData.isWebSocket = false;
            formData.requireApiKey = true;
            formData.enableCatchAll = false;
            formData.upstreamBase = "";
            formData.downstreamBase = "";
            formData.upstreamPathTemplate = "";
            formData.upstreamHttpMethod = "GET,POST";
            formData.downstreamPathTemplate = "";
            formData.downstreamScheme = "https";
            formData.priority = 1;
            formData.integrationType = "PROXY";
            formData.stripPath = true;
            formData.preserveHostHeader = false;
            formData.allowedMethods = "GET,POST,PUT,DELETE";
            formData.apiVersion = "v1";
            formData.maxRetries = 0;
            formData.retryDelayMs = 1000;
            formData.enableCircuitBreaker = false;
            formData.mockResponseCode = 200;
            formData.mockResponseBody = '{\n  "message": "Mock Response Success"\n}';
            formData.enableRateLimiting = false;
            formData.rateLimit = 100;
            formData.ratePeriodTimespan = 60;
            formData.timeFrom = null;
            formData.timeTo = null;
            formData.allowedDays = "";
            formData.loadBalancingPolicy = null;
            formData.timeoutSeconds = 30;
            formData.enableCaching = false;
            formData.cacheTtlSeconds = 60;

            selectedMethods.value = ['GET', 'POST', 'PUT', 'DELETE'];

            formData.targetHosts = [{ host: "localhost", port: 5000, weight: 1, description: "Primary Host", healthCheckPath: "/health", isHealthy: true }];
            formData.ipRules = [];
            formData.transforms = [];
            selectedDays.value = [];

            nextTick(() => {
                $('#v-basic-tab').tab('show');
            });
        };

        const Edit = (record) => {
            if ($('#dataForm').parsley()) $('#dataForm').parsley().reset();
            actionMode.value = "Edit";
            disableControl.code = true;
            ResetTabErrors();

            Object.assign(formData, record);
            formData.loadBalancingPolicy = record.loadBalancingPolicy || null;
            formData.clientIds = record.clientIds ? [...record.clientIds] : []; // <-- POPULATE ASSIGNED CLIENTS

            const catchAllRegex = /\{\*\*(catch-all|remainder)\}/i;
            formData.enableCatchAll = catchAllRegex.test(record.upstreamPathTemplate || "");

            formData.upstreamBase = CleanPath(record.upstreamPathTemplate);
            formData.downstreamBase = CleanPath(record.downstreamPathTemplate);

            const stringSource = record.allowedMethods || record.upstreamHttpMethod || "GET,POST";
            selectedMethods.value = stringSource
                .replace(/\|/g, ',')
                .split(',')
                .map(m => m.trim().toUpperCase())
                .filter(m => m);

            if (record.allowedDays) {
                selectedDays.value = record.allowedDays.split(',');
            } else {
                selectedDays.value = [];
            }

            formData.targetHosts = record.targetHosts ? [...record.targetHosts] : [];
            formData.ipRules = record.ipRules ? [...record.ipRules] : [];
            formData.transforms = record.transforms ? [...record.transforms] : [];

            nextTick(() => {
                $('#v-basic-tab').tab('show');
            });
        };

        const View = (record) => {
            viewData.value = { ...record };
            nextTick(() => {
                $('#viewModal').modal('show');
            });
        };

        const Save = async () => {
            let upBase = formData.upstreamBase ? formData.upstreamBase.replace(/\/$/, '') : "";
            let downBase = formData.downstreamBase ? formData.downstreamBase.replace(/\/$/, '') : "";

            if (formData.enableCatchAll) {
                formData.upstreamPathTemplate = upBase + "/{**catch-all}";
                formData.downstreamPathTemplate = downBase + "/{**catch-all}";
            } else {
                formData.upstreamPathTemplate = upBase;
                formData.downstreamPathTemplate = downBase;
            }

            const isValid = ValidateTabs();

            if (!isValid) {
                let errorMessages = [];
                if (tabErrors.basic > 0) errorMessages.push(`<b>General & Routing</b>: ${tabErrors.basic} field(s)`);
                if (tabErrors.security > 0) errorMessages.push(`<b>Security & Auth</b>: ${tabErrors.security} field(s)`);
                if (tabErrors.target > 0) errorMessages.push(`<b>Target Hosts</b>: ${tabErrors.target} field(s)`);
                if (tabErrors.transform > 0) errorMessages.push(`<b>Header Transforms</b>: ${tabErrors.transform} field(s)`);
                if (tabErrors.resilience > 0) errorMessages.push(`<b>Resilience & Mocking</b>: ${tabErrors.resilience} field(s)`);
                if (tabErrors.traffic > 0) errorMessages.push(`<b>Traffic & Cache</b>: ${tabErrors.traffic} field(s)`);

                swal.fire({
                    icon: 'warning',
                    title: 'Incomplete Configuration',
                    html: `Please complete required fields before saving:<br/><br/>${errorMessages.join('<br/>')}`
                });
                return;
            }

            formData.allowedMethods = selectedMethods.value.join(',');
            formData.upstreamHttpMethod = selectedMethods.value.join(',');
            formData.allowedDays = selectedDays.value.join(',');

            $(".preloader").show();
            try {
                const result = await ApiEndpointService.Save(formData);
                if (result.data.status === 'SUCCESS') {
                    $('#formModal').modal('hide');
                    swal.fire({
                        text: result.data.message,
                        icon: "success"
                    });
                    GetRecords();
                } else {
                    swal.fire({
                        icon: 'error',
                        title: 'Oops...',
                        text: result.data.message
                    });
                }
            } catch (error) {
                swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'An unexpected error occurred while saving route.'
                });
            } finally {
                $('.preloader').fadeOut('slow');
            }
        };

        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            if (search.keyword.trim() !== "") {
                params.filters.push({ "Property": "Name", "Value": search.keyword.trim(), "Operator": "Contains" });
            }
            GetRecords();
        };

        const KeyPress_Search = (e) => { if (e.which === 13) Search(); };

        const Delete = async (data) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Disable this endpoint proxy route?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, disable it!'
            });

            if (popup.value) {
                $(".preloader").show();
                try {
                    const result = await ApiEndpointService.Delete(data.id);
                    if (result.data !== '' && result.data.status === 'SUCCESS') {
                        swal.fire({ text: "Route disabled!", icon: "success" });
                        GetRecords();
                    }
                } finally {
                    $('.preloader').fadeOut('slow');
                }
            }
        };

        const Restore = async (data) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Reactivate this endpoint route?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, reactivate!'
            });

            if (popup.value) {
                $(".preloader").show();
                try {
                    const result = await ApiEndpointService.Restore(data.id);
                    if (result.data !== '' && result.data.status === 'SUCCESS') {
                        swal.fire({ text: "Route reactivated!", icon: "success" });
                        GetRecords();
                    }
                } finally {
                    $('.preloader').fadeOut('slow');
                }
            }
        };

        const AddTargetHost = () => {
            formData.targetHosts.push({ host: "localhost", port: 5000, weight: 1, description: "", healthCheckPath: "/health", isHealthy: true });
            nextTick(() => ValidateTabs());
        };

        const RemoveTargetHost = (index) => {
            formData.targetHosts.splice(index, 1);
            nextTick(() => ValidateTabs());
        };

        const AddIpRule = () => {
            formData.ipRules.push({ ipAddressOrRange: "127.0.0.1", ruleType: "Allow", description: "" });
            nextTick(() => ValidateTabs());
        };

        const RemoveIpRule = (index) => {
            formData.ipRules.splice(index, 1);
            nextTick(() => ValidateTabs());
        };

        const AddTransform = () => {
            formData.transforms.push({ transformPhase: "Request", action: "Add", headerName: "", headerValue: "" });
            nextTick(() => ValidateTabs());
        };

        const RemoveTransform = (index) => {
            formData.transforms.splice(index, 1);
            nextTick(() => ValidateTabs());
        };

        const ActionModeIcon = () => actionMode.value === 'Add' ? 'fa-plus-circle' : 'fa-edit';

        return {
            actionMode,
            pageSizeArray,
            params,
            show_table,
            disableControl,
            search,
            formData,
            records,
            categories,
            authProviders,
            outboundAuthProfiles,
            clients, // <-- EXPORTED PARA SA UI DROPDOWN
            selectedDays,
            availableMethods,
            selectedMethods,
            getMethodBadgeClass,
            tabErrors,
            totalErrors,
            viewData,
            parsedViewMethods,
            ItemCountChange: () => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,
            KeyPress_Search,
            ActionModeIcon,
            Add,
            Edit,
            View,
            Delete,
            Save,
            Restore,
            AddTargetHost,
            RemoveTargetHost,
            AddIpRule,
            RemoveIpRule,
            AddTransform,
            RemoveTransform
        };
    }
});

controller.mount('#controller');