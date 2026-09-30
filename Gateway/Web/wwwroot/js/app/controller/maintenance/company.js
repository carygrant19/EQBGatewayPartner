const { createApp, ref, onMounted } = Vue;

const controller = createApp({
    setup() {
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
        });
        params.sortColumn = "Description"; 
        const GetRecords = async () => {
            $(".preloader").show();
            try {
                const result = await CompanyService.Search(params);
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
                params.filters.push({ "Property": "Description", "Value": search.description.trim(), "Operator": "Contains" });
            }
            GetRecords();
        };

        const Add = () => {
            actionMode.value = "Add";
            $('#form').parsley().reset();
            disableControl.code = false;

            formData.id = '';
            formData.code = '';
            formData.description = '';
        };

        const Edit = (record) => {
            actionMode.value = "Edit";
            $('#form').parsley().reset();
            disableControl.code = true;

            formData.id = record.id;
            formData.code = record.code;
            formData.description = record.description;
        };

        const Save = async () => {
            if ($('#form').parsley().validate()) {
                $(".preloader").show();
                try {
                    const result = await CompanyService.Save(formData);
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
                text: "Company '" + record.code + "' will be deleted!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await CompanyService.Delete(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Company successfully deleted!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to delete company!" });
                }
            }
        };

        const Restore = async (record) => {
            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Restore company '" + record.code + "'?",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const payload = { id: record.id, code: record.code };
                const result = await CompanyService.Restore(payload);
                $('.preloader').fadeOut('slow');

                if (result.data && result.data.status === 'SUCCESS') {
                    swal.fire({ text: "Company successfully restored!", icon: "success" });
                    GetRecords();
                } else {
                    swal.fire({ icon: 'error', text: result.data ? result.data.message : "Failed to restore company!" });
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
            ItemCountChange: (col) => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,
            ActionModeIcon,
            Add,
            Edit,
            Delete,
            Save,
            Restore, 
        };
        return {
            ...returnProps,
            ...returnMethod

        };
    }
});

controller.mount('#CompanyController');