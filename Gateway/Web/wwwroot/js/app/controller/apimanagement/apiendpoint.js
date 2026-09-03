const { createApp, reactive, ref, computed, onMounted, nextTick } = Vue;

const controller = createApp({
    setup() {
        const records = ref([]);
        const categories = ref([]);
        const show_table = ref(false);
        const actionMode = ref("Add");

        const tabErrors = reactive({
            basic: 0,
            target: 0,
            traffic: 0,
            security: 0
        });

        const totalErrors = computed(() => {
            return tabErrors.basic + tabErrors.target + tabErrors.traffic + tabErrors.security;
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
            isActive: true,
            isWebSocket: false,
            requireApiKey: true,
            upstreamPathTemplate: "",
            upstreamHttpMethod: "GET",
            downstreamPathTemplate: "",
            downstreamScheme: "https",
            priority: 1,
            enableRateLimiting: false,
            rateLimit: 100,
            ratePeriodTimespan: 60,
            timeFrom: null,
            timeTo: null,
            dateFrom: null,
            dateTo: null,
            allowedDays: "",
            loadBalancingPolicy: "RoundRobin",
            timeoutSeconds: 30,
            enableCaching: false,
            cacheTtlSeconds: 60,
            allowedOrigins: "*",
            targetHosts: [],
            ipRules: []
        });

        onMounted(() => {
            global.GetRecords = GetRecords;
            GetCategories();
            GetRecords();
        });

        const GetCategories = async () => {
            try {
                const response = await ApiEndpointService.Categories();
                if (response.data) categories.value = response.data;
            } catch (error) {
                console.error("Failed to load categories", error);
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
            tabErrors.target = countErrorsInTab('#v-target');
            tabErrors.traffic = countErrorsInTab('#v-traffic');
            tabErrors.security = countErrorsInTab('#v-security');

            return totalErrors.value === 0;
        };

        const Add = () => {
            if ($('#dataForm').parsley()) $('#dataForm').parsley().reset();
            actionMode.value = "Add";
            disableControl.code = false;

            tabErrors.basic = 0;
            tabErrors.target = 0;
            tabErrors.traffic = 0;
            tabErrors.security = 0;

            formData.id = "0";
            formData.code = "";
            formData.name = "";
            formData.description = "";
            formData.categoryId = categories.value.length > 0 ? categories.value[0].id : null;
            formData.isActive = true;
            formData.isWebSocket = false;
            formData.requireApiKey = true;
            formData.upstreamPathTemplate = "";
            formData.upstreamHttpMethod = "GET";
            formData.downstreamPathTemplate = "";
            formData.downstreamScheme = "https";
            formData.priority = 1;
            formData.enableRateLimiting = false;
            formData.rateLimit = 100;
            formData.ratePeriodTimespan = 60;
            formData.timeFrom = null;
            formData.timeTo = null;
            formData.allowedDays = "";
            formData.loadBalancingPolicy = "RoundRobin";
            formData.timeoutSeconds = 30;
            formData.enableCaching = false;
            formData.cacheTtlSeconds = 60;

            formData.targetHosts = [{ host: "localhost", port: 5000, weight: 1, description: "Primary" }];
            formData.ipRules = [];
            selectedDays.value = [];

            nextTick(() => {
                $('#v-basic-tab').tab('show');
            });
        };

        const Edit = (record) => {
            if ($('#dataForm').parsley()) $('#dataForm').parsley().reset();
            actionMode.value = "Edit";
            disableControl.code = true;

            tabErrors.basic = 0;
            tabErrors.target = 0;
            tabErrors.traffic = 0;
            tabErrors.security = 0;

            Object.assign(formData, record);

            if (record.allowedDays) {
                selectedDays.value = record.allowedDays.split(',');
            } else {
                selectedDays.value = [];
            }

            formData.targetHosts = record.targetHosts ? [...record.targetHosts] : [];
            formData.ipRules = record.ipRules ? [...record.ipRules] : [];

            nextTick(() => {
                $('#v-basic-tab').tab('show');
            });
        };

        const Save = async () => {
            const isValid = ValidateTabs();

            if (!isValid) {
                let errorMessages = [];
                if (tabErrors.basic > 0) errorMessages.push(`<b>General & Routing</b>: ${tabErrors.basic} issue(s)`);
                if (tabErrors.target > 0) errorMessages.push(`<b>Target Hosts</b>: ${tabErrors.target} issue(s)`);
                if (tabErrors.traffic > 0) errorMessages.push(`<b>Traffic & Cache</b>: ${tabErrors.traffic} issue(s)`);
                if (tabErrors.security > 0) errorMessages.push(`<b>Access & IP Rules</b>: ${tabErrors.security} issue(s)`);

                swal.fire({
                    icon: 'warning',
                    title: 'Incomplete Configuration',
                    html: `Please complete required fields before saving:<br/><br/>${errorMessages.join('<br/>')}`
                });
                return;
            }

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
                    text: 'An unexpected error occurred.'
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
            formData.targetHosts.push({ host: "localhost", port: 5000, weight: 1, description: "" });
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
            selectedDays,
            tabErrors,
            totalErrors,
            ItemCountChange: () => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,
            KeyPress_Search,
            ActionModeIcon,
            Add,
            Edit,
            Delete,
            Save,
            Restore,
            AddTargetHost,
            RemoveTargetHost,
            AddIpRule,
            RemoveIpRule
        };
    }
});

controller.mount('#controller');