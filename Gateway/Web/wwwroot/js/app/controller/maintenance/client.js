const { createApp, ref, onMounted } = Vue;

const controller = createApp({
    setup() {
        const companies = ref([]);

        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
            GetCompanies();
        });

        params.sortColumn = "Name";

        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await ClientService.Search(params);
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
                    else
                        $('#sync-pagination').twbsPagination('destroy');
                }
                else {
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

        const GetCompanies = async () => {
            if (typeof CompanyService !== 'undefined' && CompanyService.All) {
                const result = await CompanyService.All();
                if (result.data) {
                    companies.value = result.data;
                }
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

        const Add = () => {
            actionMode.value = "Add";
            $('#dataForm').parsley().reset();
            disableControl.code = false;

            formData.id = '';
            formData.code = '';
            formData.name = '';
            formData.companyId = '';
            formData.description = '';
            formData.sslRequired = false;
        };

        const Edit = (record) => {
            actionMode.value = "Edit";
            $('#dataForm').parsley().reset();
            disableControl.code = true;

            formData.id = record.id;
            formData.code = record.code;
            formData.name = record.name;
            formData.companyId = record.companyId;
            formData.description = record.description;
            formData.sslRequired = record.sslRequired;
        };

        const Save = async () => {
            if ($('#dataForm').parsley().validate()) {
                $(".preloader").show();
                try {
                    const result = await ClientService.Save(formData);
                    $('.preloader').fadeOut('slow');

                    if (result.data && result.data.status === 'SUCCESS') {
                        $('#formModal').modal('hide');

                        let msg = result.data.message;
                        if (msg.includes('|Key:')) {
                            let parts = msg.split('|');
                            let key = parts[1].replace('Key:', '');
                            let secret = parts[2].replace('Secret:', '');

                            let htmlContent = [
                                '<div class="text-start mt-2 fs-14">',
                                '<p class="mb-1"><strong>Client Name:</strong> ', formData.name, '</p>',
                                '<p class="mb-1"><strong>API Key:</strong> <code class="text-primary">', key, '</code></p>',
                                '<p class="mb-1"><strong>API Secret:</strong> <code class="text-danger">', secret, '</code></p>',
                                '<small class="text-muted">Please save these credentials securely. The API Secret will not be shown again.</small>',
                                '</div>'
                            ].join('');

                            swal.fire({
                                title: "Client Created Successfully!",
                                html: htmlContent,
                                icon: "success"
                            });
                        } else {
                            swal.fire({ text: msg, icon: "success" });
                        }
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
                text: "Client '" + record.code + "' will be deactivated!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await ClientService.Delete(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Client successfully disabled!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to delete client!" });
                }
            }
        };

        const Restore = async (record) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Restore client '" + record.code + "' access?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await ClientService.Restore(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Client successfully restored!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to restore client!" });
                }
            }
        };

        const ResetCredentials = async (record) => {
            const popup = await swal.fire({
                title: "Reset Credentials?",
                text: "Generate new API Key & Secret for '" + record.code + "'? The old credentials will stop working immediately.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Yes, Reset Now!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await ClientService.ResetCredentials(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    let htmlContent = [
                        '<div class="text-start mt-2 fs-14">',
                        '<p class="mb-1"><strong>Client:</strong> ', record.code, '</p>',
                        '<p class="mb-1"><strong>New API Key:</strong> <code class="text-primary">', result.data.key, '</code></p>',
                        '<p class="mb-1"><strong>New API Secret:</strong> <code class="text-danger">', result.data.secret, '</code></p>',
                        '<small class="text-muted">Save these credentials now. The secret will not be displayed again.</small>',
                        '</div>'
                    ].join('');

                    swal.fire({
                        title: "Credentials Reset Successfully!",
                        html: htmlContent,
                        icon: "success"
                    });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to reset credentials!" });
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
            companies,
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
            ResetCredentials
        };

        return {
            ...returnProps,
            ...returnMethod
        };
    }
});

controller.mount('#controller');


