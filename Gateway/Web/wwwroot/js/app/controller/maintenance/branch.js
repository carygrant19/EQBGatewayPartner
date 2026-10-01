const { createApp, reactive, ref, computed, onMounted, filter } = Vue;
const controller = createApp({
    setup() {
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
        });
        params.sortColumn = "Description";  

        //CRUD
        const GetRecords = async () => {

            $(".preloader").show();

            const result = await BranchService.Search(params);

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

            $('.preloader').fadeOut('slow');


        };
         
        const Delete = async (data) => {

            const popup = await swal.fire({
                title: "Are you sure?",
                text: "Once delete, this will not be accessible on the system!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const result = await BranchService.Delete(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Branch '" + data.code + "' successfully deleted!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to delete branch!"
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }
        };

        const Restore = async (data) => {

            const popup = await swal.fire({
                title: "Are you sure?",
                text: "You want to restore this record.",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, restore it!'
            });

            if (popup.value) {
                $(".preloader").show();
                const result = await BranchService.Restore(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Branch '" + data.code + "' successfully restored!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to restore branch!"
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }

        };

        const Save = async () => {
            if ($('#dataForm').parsley().validate()) {
                $(".preloader").show();
                const result = await BranchService.Save(formData);

                if (result.data.status === 'SUCCESS') {
                    $('#formModal').modal('hide');
                    swal.fire({
                        text: result.data.message,
                        icon: "success"
                    });
                    GetRecords();
                }
                else {
                    swal.fire({
                        icon: 'error',
                        title: 'Oops...',
                        text: result.data.message
                    });
                }
                $('.preloader').fadeOut('slow');
            }
        };

        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            if ($('#searchDescription').val().trim() !== "")
                params.filters.push({ "Property": "Description", "Value": search.description, "Operator": "Contains" });
            GetRecords();
        };
 
        const Add = () => {
            $('#dataForm').parsley().reset();
            disableControl.code = false;
            formData.id = '';
            formData.code = '';
            formData.description = '';
            formData.bankingHour = '';
            formData.officer = '';
            formData.address = '';
            formData.contactNo = '';
            formData.email = '';

        };

        const Edit = (record) => {
            $('#dataForm').parsley().reset();
            disableControl.code = true;
            formData.id = record.id;
            formData.code = record.code;
            formData.description = record.description;
            formData.bankingHour = record.bankingHour;
            formData.officer = record.officer;
            formData.address = record.address;
            formData.contactNo = record.contactNo;
            formData.email = record.email;
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

        // Return methods
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
controller.mount('#controller');
