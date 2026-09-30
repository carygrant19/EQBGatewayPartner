const { createApp, ref, onMounted } = Vue;

const controller = createApp({
    setup() {
        onMounted(() => {
            GetRecords();
        });

        params.sortColumn = "Name";

        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await AuthProviderService.Search(params);
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
            $('#form').parsley().reset();
            disableControl.code = false;

            formData.id = '';
            formData.code = '';
            formData.name = '';
            formData.issuer = '';
            formData.audience = '';
            formData.secretKey = '';
            formData.tokenLifetimeMinutes = 60;
            formData.isActive = true;
        };

        const Edit = (record) => {
            actionMode.value = "Edit";
            $('#form').parsley().reset();
            disableControl.code = true;

            formData.id = record.id;
            formData.code = record.code;
            formData.name = record.name;
            formData.issuer = record.issuer;
            formData.audience = record.audience;
            formData.secretKey = record.secretKey;
            formData.tokenLifetimeMinutes = record.tokenLifetimeMinutes;
            formData.isActive = record.isActive;
        };

        const Save = async () => {
            if ($('#form').parsley().validate()) {
                $(".preloader").show();
                try {
                    const result = await AuthProviderService.Save(formData);
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
                text: "Auth Provider '" + record.code + "' will be deactivated!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, deactivate it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await AuthProviderService.Delete(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Auth Provider successfully deactivated!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to deactivate Auth Provider!" });
                }
            }
        };

        const Restore = async (record) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Reactivate Auth Provider '" + record.code + "'?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await AuthProviderService.Restore(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Auth Provider successfully restored!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to restore Auth Provider!" });
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
            Restore
        };

        return {
            ...returnProps,
            ...returnMethod
        };
    }
});

controller.mount('#AuthProviderController');