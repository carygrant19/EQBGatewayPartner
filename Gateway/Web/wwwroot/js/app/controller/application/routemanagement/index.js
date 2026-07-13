const { createApp, reactive, ref, computed, onMounted } = Vue;

const controller = createApp({
    setup() {
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
            GetClients();
            GetCategories();
            search.codeOrName = '';
            search.upstreamPath = '';
            formData.category = '';
        });

        params.sortColumn = "Description";

        const isEditMode = ref(false);
        const isSubmitting = ref(false);
        const modalTab = ref('general');
        const selectedDays = ref([]);
        const clients = ref([]);
        const categories = ref([]);
        const selectedClientId = ref('');

        // REACTIVE OBJECT TO TRACK WHICH TABS ARE INVALID
        const tabErrors = reactive({
            general: false,
            hosts: false,
            ipmanagement: false,
            clients: false,
            qos: false,
            advanced: false
        });

        const GetRecords = async () => {
            $(".preloader").show();

            try {
                const result = await RouteService.Search(params);

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
                    else {
                        if ($('#sync-pagination').data("twbs-pagination")) {
                            $('#sync-pagination').twbsPagination('destroy');
                        }
                    }
                }
                else {
                    records.value = [];
                    show_table.value = false;
                }
            } catch (error) {
                console.error("Failed to query gateway records:", error);
                records.value = [];
                show_table.value = false;
            } finally {
                $('.preloader').fadeOut('slow');
            }
        };

        const GetClients = async () => {
            const result = await ClientService.All();

            if (result.data != null) {
                clients.value = result.data;
            }
            else {
                clients.value = [];
            }
        };
        const GetCategories = async () => {
            const result = await RouteService.AllCategory();

            if (result.data != null) {
                categories.value = result.data;
            }
            else {
                categories.value = [];
            }
        };

        const Publish = async () => {
            swal.fire({
                title: "Are you sure?",
                text: "Once published, this will be immediately in effect",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, publish it!'
            }).then((result) => {
                if (result.isConfirmed) {
                    RouteService.PublishOcelot()
                        .then((result) => {
                            if (result.data.status === 'SUCCESS') {
                                swal.fire({
                                    text: "Route successfully published!",
                                    icon: "success"
                                });

                                Search();
                            }
                            else if (result.data.status === 'FAILED') {
                                swal.fire({
                                    icon: 'error',
                                    text: result.data.message
                                });
                            }
                            else {
                                swal.fire({
                                    icon: 'error',
                                    title: 'Oops...',
                                    text: 'Error encountered'
                                });
                            }
                        });
                }
            });
        }
        const Search = async () => {
            params.filters = [];
            params.pageNum = 1;

            if (search.codeOrName.trim() !== "") {
                params.filters.push({ "Property": "Name", "Value": search.codeOrName.trim(), "Operator": "Contains" });
            }

            if (search.upstreamPath.trim() !== "") {
                params.filters.push({ "Property": "UpstreamPathTemplate", "Value": search.upstreamPath.trim(), "Operator": "Contains" });
            }

            await GetRecords();
        };

        const Add = () => {
            isEditMode.value = false;
            modalTab.value = 'general';
            selectedClientId.value = '';
            resetForm();

            if ($('#modalRouteForm').data('parsley')) {
                $('#modalRouteForm').parsley().reset();
            }
            $('.parsley-error').removeClass('parsley-error');
            $('.parsley-errors-list').remove();

            var myModal = new bootstrap.Modal(document.getElementById('routeModal'));
            myModal.show();
        };

        const openCreateModal = () => {
            Add();
        };

        const Edit = (record) => {
            isEditMode.value = true;
            modalTab.value = 'general';
            selectedClientId.value = '';

            tabErrors.general = false;
            tabErrors.hosts = false;
            tabErrors.ipmanagement = false;
            tabErrors.clients = false;
            tabErrors.qos = false;
            tabErrors.advanced = false;

            if ($('#modalRouteForm').data('parsley')) {
                $('#modalRouteForm').parsley().reset();
            }
            $('.parsley-error').removeClass('parsley-error');
            $('.parsley-errors-list').remove();

            Object.assign(formData, JSON.parse(JSON.stringify(record)));

            formData.hosts = formData.hosts || [];
            formData.ipRules = formData.ipRules || [];
            formData.clients = formData.clients || [];

            if (formData.allowedDays) {
                selectedDays.value = formData.allowedDays.split(',');
            } else {
                selectedDays.value = [];
            }

            var myModal = new bootstrap.Modal(document.getElementById('routeModal'));
            myModal.show();
        };

        const View = (record) => {
            window.location.href = appUrl + "/Application/RouteManagement/Details/" + record.id;
        };

        const addHostRow = () => {
            formData.hosts.push({ host: '', port: 443, description: '' });
        };

        const updateWebSocketState = () => {
            const scheme = formData.downstreamScheme;
            formData.isWebSocket = (scheme === 'ws' || scheme === 'wss');
        };

        const syncDaysToString = () => {
            formData.allowedDays = selectedDays.value.join(',');
        };

        const addClientInline = () => {
            if (!selectedClientId.value) return;

            if (!formData.clients) {
                formData.clients = [];
            }

            const alreadyExists = formData.clients.some(c => c.clientId === selectedClientId.value);
            if (alreadyExists) {
                swal.fire({
                    icon: 'warning',
                    text: 'This client profile account has already been whitelisted onto the route.'
                });
                return;
            }

            formData.clients.push({
                id: 0,
                routeId: formData.id,
                clientId: selectedClientId.value
            });

            selectedClientId.value = '';
        };

        const getClientMetadata = (clientId) => {
            return clients.value.find(c => c.id === clientId) || {};
        };

        const triggerFormSubmit = () => {
            Save();
        };

        const Save = async () => {
            const parsleyInstance = $('#modalRouteForm').parsley();

            const isParsleyValid = parsleyInstance.validate();
            const isClientsValid = formData.clients && formData.clients.length > 0;
            // Evaluates dynamic destination server row array counts explicitly
            const isHostsValid = formData.hosts && formData.hosts.length > 0;
            const isValid = isParsleyValid && isClientsValid && isHostsValid;

            tabErrors.general = false;
            tabErrors.hosts = false;
            tabErrors.ipmanagement = false;
            tabErrors.clients = false;
            tabErrors.qos = false;
            tabErrors.advanced = false;

            if (isValid) {
                isSubmitting.value = true;
                $(".preloader").show();

                try {
                    const result = await RouteService.Save(formData);

                    if (result.data.status === 'SUCCESS') {
                        $('#routeModal').modal('hide');
                        swal.fire({
                            text: result.data.message || "Gateway configuration committed successfully!",
                            icon: "success"
                        });
                        resetForm();
                        await GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            title: 'Validation Conflict',
                            text: result.data.message
                        });
                    }
                } catch (error) {
                    console.error(error);
                    swal.fire({
                        icon: 'error',
                        text: "A background connection fault occurred."
                    });
                } finally {
                    isSubmitting.value = false;
                    $('.preloader').fadeOut('slow');
                }
            } else {
                parsleyInstance.fields.forEach(field => {
                    if (!field.isValid()) {
                        const element = $(field.$element);
                        const parentPane = element.closest('[data-tab-name]');
                        if (parentPane.length > 0) {
                            const tabName = parentPane.attr('data-tab-name') || '';

                            if (tabName === 'general') tabErrors.general = true;
                            if (tabName === 'hosts') tabErrors.hosts = true;
                            if (tabName === 'ipmanagement') tabErrors.ipmanagement = true;
                            if (tabName === 'clients') tabErrors.clients = true;
                            if (tabName === 'qos') tabErrors.qos = true;
                            if (tabName === 'advanced') tabErrors.advanced = true;
                        }
                    }
                });

                if (!isClientsValid) {
                    tabErrors.clients = true;
                }
                // Flags tab 2 badge explicitly if empty
                if (!isHostsValid) {
                    tabErrors.hosts = true;
                }

                let incompleteSections = [];
                if (tabErrors.general) incompleteSections.push("<li><b>1. Basic Settings</b></li>");
                if (tabErrors.hosts) incompleteSections.push("<li><b>2. Destination API Servers</b></li>");
                if (tabErrors.ipmanagement) incompleteSections.push("<li><b>3. IP Firewall Rules Matrix</b></li>");
                if (tabErrors.clients) incompleteSections.push("<li><b>4. Whitelisted Client Lanes</b></li>");
                if (tabErrors.qos) incompleteSections.push("<li><b>5. Limits, Cache & QoS Settings</b></li>");
                if (tabErrors.advanced) incompleteSections.push("<li><b>6. Active Schedule Range Lanes</b></li>");

                if (tabErrors.general) modalTab.value = 'general';
                else if (tabErrors.hosts) modalTab.value = 'hosts';
                else if (tabErrors.ipmanagement) modalTab.value = 'ipmanagement';
                else if (tabErrors.clients) modalTab.value = 'clients';
                else if (tabErrors.qos) modalTab.value = 'qos';
                else if (tabErrors.advanced) modalTab.value = 'advanced';

                swal.fire({
                    icon: 'warning',
                    title: 'Form Submission Blocked',
                    html: `<div class="text-start">Please complete required or missing fields in these section(s):<ul class="mt-2 text-danger">${incompleteSections.join('')}</ul></div>`,
                    confirmButtonColor: '#0d6efd'
                });

                setTimeout(() => {
                    const errorField = $('.parsley-error').first();
                    if (errorField.length > 0) {
                        errorField.focus();
                    }
                }, 180);
            }
        };

        const Delete = async (data) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Once deactivated, this route configuration will not process incoming traffic!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, deactivate it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const result = await RouteService.Delete(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Route '" + data.code + "' successfully deactivated!",
                            icon: "success"
                        });
                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Failed to deactivate route: " + result.data.message
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }
        };

        const Restore = async (data) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "You want to restore and bring this route back online.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const result = await RouteService.Restore(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Route '" + data.code + "' successfully restored online!",
                            icon: "success"
                        });
                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Failed to restore route!"
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }
        };

        const resetForm = () => {
            selectedDays.value = ['0', '1', '2', '3', '4', '5', '6'];

            tabErrors.general = false;
            tabErrors.hosts = false;
            tabErrors.ipmanagement = false;
            tabErrors.clients = false;
            tabErrors.qos = false;
            tabErrors.advanced = false;

            formData.id = 0;
            formData.routeGroup = 'COMMON';
            formData.code = '';
            formData.name = '';
            formData.description = '';
            formData.isActive = true;
            formData.isWebSocket = false;
            formData.upstreamPathTemplate = '';
            formData.upstreamHttpMethod = 'GET';
            formData.downstreamPathTemplate = '';
            formData.downstreamScheme = 'https';
            formData.downstreamHttpVersion = '1.1';
            formData.upstreamHost = '';
            formData.authenticationProviderKey = '';
            formData.routeIsCaseSensitive = false;
            formData.dangerousAcceptAnyServerCertificateValidator = false;
            formData.priority = 1;
            formData.enableRateLimiting = false;
            formData.rateLimit = null;
            formData.ratePeriod = '';
            formData.ratePeriodTimespan = null;
            formData.rateLimitHttpStatusCode = 429;
            formData.rateLimitQuotaExceededMessage = '';
            formData.enableCaching = false;
            formData.cacheTtlSeconds = null;
            formData.enableQoS = false;
            formData.qoSTimeoutMs = 30000;
            formData.qoSExceptionsAllowedBeforeBreaking = 3;
            formData.qoSDurationOfBreakMs = 10000;
            formData.loadBalancerType = 'RoundRobin';
            formData.loadBalancerKey = '';
            formData.loadBalancerExpiryMs = 0;
            formData.requireSignature = false;
            formData.enableTimeLimit = false;
            formData.timeFrom = '';
            formData.timeTo = '';
            formData.allowedDays = '0,1,2,3,4,5,6';
            formData.useServiceDiscovery = false;
            formData.serviceName = '';
            formData.serviceNamespace = '';
            formData.enableServicePolling = false;
            formData.pollingIntervalMs = 300;
            formData.hosts = [];
            formData.ipRules = [];
            formData.clients = [];
        };

        const FilterForm = () => {
            search.codeOrName = '';
            search.upstreamPath = '';
        };

        return {
            actionMode,
            pageSizeArray,
            params,
            show_table,
            disableControl,
            search,
            formData,
            records,
            modalTab,
            isEditMode,
            isSubmitting,
            selectedDays,
            clients,
            categories,
            selectedClientId,
            tabErrors,
            ItemCountChange: (col) => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,
            View,
            Add,
            Edit,
            Delete,
            Restore,
            Save,
            openCreateModal,
            addHostRow,
            triggerFormSubmit,
            resetForm,
            FilterForm,
            updateWebSocketState,
            syncDaysToString,
            addClientInline,
            getClientMetadata,
            getBadgeClass,
            Publish,
            Filter: () => { Search(); $('#filterModal').modal('hide'); },
            validateFieldInline: (fieldName) => {
                setTimeout(() => {
                    $(`[name="${fieldName}"]`).parsley().validate();
                }, 10);
            },
        };
    }
});

global.registerFormatPlugins(controller);
controller.mount('#controller');