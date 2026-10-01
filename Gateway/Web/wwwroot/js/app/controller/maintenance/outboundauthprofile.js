const { createApp, ref, onMounted, reactive } = Vue;

const controller = createApp({
    setup() {
        onMounted(() => {
            GetRecords();
        });

        params.sortColumn = "Name";

        const formData = reactive({
            id: '',
            code: '',
            name: '',
            description: '',
            isActive: true,
            headers: []
        });

        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await OutboundAuthProfileService.Search(params);
                if (result.data && result.data.totalRecord > 0) {
                    records.value = result.data.data;
                    show_table.value = true;

                    if (params.pageNum > result.data.totalPage) {
                        params.pageNum = Math.max(1, result.data.totalPage);
                        GetRecords();
                    }
                } else {
                    records.value = [];
                    show_table.value = false;
                }
            } catch (error) {
                console.error(error);
                records.value = [];
                show_table.value = false;
            } finally {
                $('.preloader').fadeOut('slow');
            }
        };

        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            if (search.description && search.description.trim() !== "") {
                params.filters.push({ "Property": "Name", "Value": search.description.trim(), "Operator": "Contains" });
            }
            GetRecords();
        };

        const AddHeader = () => {
            if (!formData.headers || !Array.isArray(formData.headers)) {
                formData.headers = [];
            }
            formData.headers.push({
                authType: 'APIKey',
                headerName: '',
                credentialValue: '',
                secondaryCredentialValue: ''
            });
        };

        const RemoveHeader = (index) => {
            if (formData.headers && formData.headers.length > 0) {
                formData.headers.splice(index, 1);
            }
        };

        const Add = () => {
            actionMode.value = "Add";
            $('#form').parsley().reset();
            disableControl.code = false;

            formData.id = '';
            formData.code = '';
            formData.name = '';
            formData.description = '';
            formData.isActive = true;
            formData.headers = [];

            AddHeader(); // Lagyan ng 1 default blank row
        };

        const Edit = (record) => {
            actionMode.value = "Edit";
            $('#form').parsley().reset();
            disableControl.code = true;

            formData.id = record.id;
            formData.code = record.code;
            formData.name = record.name;
            formData.description = record.description;
            formData.isActive = record.isActive;

            formData.headers = record.headers && Array.isArray(record.headers)
                ? JSON.parse(JSON.stringify(record.headers))
                : [];

            if (formData.headers.length === 0) {
                AddHeader();
            }
        };

        const Save = async () => {
            if ($('#form').parsley().validate()) {
                if (formData.headers && formData.headers.length > 0) {
                    // 1. Validation: Suriin kung may magkaparehong Auth Type
                    const authTypes = formData.headers
                        .map(h => (h.authType || '').trim().toUpperCase())
                        .filter(h => h !== '');

                    const duplicateAuthType = authTypes.find((type, index) => authTypes.indexOf(type) !== index);

                    if (duplicateAuthType) {
                        swal.fire({
                            icon: 'error',
                            title: 'Duplicate Auth Type',
                            text: 'Auth Type "' + duplicateAuthType + '" is selected more than once! Each header must have a unique Auth Type.'
                        });
                        return;
                    }

                    // 2. Validation: Suriin kung may magkaparehong Header Name
                    const headerNames = formData.headers
                        .map(h => (h.headerName || '').trim().toUpperCase())
                        .filter(h => h !== '');

                    const duplicateHeaderName = headerNames.find((name, index) => headerNames.indexOf(name) !== index);

                    if (duplicateHeaderName) {
                        swal.fire({
                            icon: 'error',
                            title: 'Duplicate Header Name',
                            text: 'Header Name "' + duplicateHeaderName + '" is defined more than once! Each header name must be unique.'
                        });
                        return;
                    }
                }

                $(".preloader").show();
                try {
                    const result = await OutboundAuthProfileService.Save(formData);
                    $('.preloader').fadeOut('slow');

                    if (result.data && result.data.status === 'SUCCESS') {
                        $('#formModal').modal('hide');
                        swal.fire({ text: result.data.message, icon: "success" });
                        GetRecords();
                    } else {
                        swal.fire({ icon: 'error', title: 'Oops...', text: result.data ? result.data.message : 'Save failed' });
                    }
                } catch (ex) {
                    $('.preloader').fadeOut('slow');
                    swal.fire({ icon: 'error', title: 'Error', text: 'Error encountered during save.' });
                }
            }
        };

        const Delete = async (record) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Outbound Auth Profile '" + record.code + "' will be deactivated!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, deactivate it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await OutboundAuthProfileService.Delete(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Profile successfully deactivated!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to deactivate profile!" });
                }
            }
        };

        const Restore = async (record) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Reactivate Outbound Auth Profile '" + record.code + "'?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await OutboundAuthProfileService.Restore(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Profile successfully restored!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to restore profile!" });
                }
            }
        };

        const ItemCountChange = () => {
            params.pageNum = 1;
            GetRecords();
        };

        const Sort = (col) => {
            if (params.sortColumn === col) {
                params.descending = !params.descending;
            } else {
                params.sortColumn = col;
                params.descending = false;
            }
            GetRecords();
        };

        const SortClass = (col) => {
            if (params.sortColumn !== col) return 'fa-sort text-muted';
            return params.descending ? 'fa-sort-down text-dark' : 'fa-sort-up text-dark';
        };

        const ActionModeIcon = () => {
            return actionMode.value === 'Add' ? 'fa-plus-circle' : 'fa-edit';
        };

        const returnProps = {
            actionMode,
            pageSizeArray,
            params,
            show_table,
            disableControl,
            search,
            formData,
            records,
        };

        const returnMethod = {
            ItemCountChange: () => ItemCountChange(),
            Sort: (col) => Sort(col),
            SortClass: (col) => SortClass(col),
            Search,
            ActionModeIcon,
            Add,
            Edit,
            Delete,
            Save,
            Restore,
            AddHeader,
            RemoveHeader
        };

        return {
            ...returnProps,
            ...returnMethod
        };
    }
});

controller.mount('#OutboundAuthProfileController');