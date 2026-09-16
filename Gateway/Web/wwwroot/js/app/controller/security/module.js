const { createApp, reactive, ref, computed, onMounted, filter } = Vue;
const controller = createApp({
    setup() { 
        onMounted(() => { 
            global.GetRecords = GetRecords;
            GetRecords();
            GetModulePermission();
            GetParent();
        }); 
        params.sortColumn = "Description"; 

        var selectedPermissions = [];
        var concatenatedValues = []; 
        const parentIds = ref([]); 
        const modulePermissions = ref([]); 
       
        //CRUD
        const GetModulePermission = async () => {
            const result = await PermissionService.All()
            if (result.data != null) {
                modulePermissions.value = result.data;
            }
            else {
                modulePermissions.value = [];
            }
        }; 
        const GetParent = async () => {
            const result = await ModuleService.AllParent()
            if (result.data != null) {
                parentIds.value = result.data;
            }
            else {
                parentIds.value = [];
            }
        }; 
        const GetRecords = async () => {

            $(".preloader").show();

            const result = await ModuleService.Search(params);
                 
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
                const result = await ModuleService.Delete(data.id); 
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Module '" + data.code + "' successfully deleted!",
                            icon: "success"
                        });

                        GetRecords();
                        GetParent();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to delete module!"
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

            if (popup.value)
            {
                $(".preloader").show();
                const result = await ModuleService.Restore(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Module '" + data.code + "' successfully restored!",
                            icon: "success"
                        });

                        GetRecords();
                        GetParent();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to restore module!"
                        });
                    }
                } 
                $('.preloader').fadeOut('slow');
            }
            
        };

        const Save = async () => {
            if ($('#dataForm').parsley().validate()) {
                $(".preloader").show();
                GetCheckedValues();
                formData.permission = concatenatedValues;
                const result = await ModuleService.Save(formData);
                   
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
                GetParent();

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
            formData.parentId = '';
            formData.code = '';
            formData.name = '';
            formData.moduleType = '';
            formData.displayOrder = '';
            formData.description = '';
            formData.url = '';
            formData.icon = '';
            formData.auditContent = '';
            formData.show = false;
            UncheckAll();
        };

        const Edit = (record) => {
            $('#dataForm').parsley().reset();
            UncheckAll();
            disableControl.code = true;
            formData.id = record.id;
            formData.parentId = '';
            for (let x = 0; x < parentIds.value.length; x++) {
                if (parentIds.value[x].id == record.parentId) {
                    formData.parentId = record.parentId;
                }
            } 
            formData.code = record.code;
            formData.name = record.name;
            formData.moduleType = record.moduleType;
            formData.displayOrder = record.displayOrder;
            formData.description = record.description;
            formData.url = record.url;
            formData.icon = record.icon;
            formData.auditContent = record.auditContent;
            formData.show = record.show;
            selectedPermissions = record.modulePermission.split('|');
            CheckPermissions();
        };
        const CheckPermissions = () => {
            // Iterate through each checkbox and check/uncheck based on selectedPermissions
            var checkboxes = document.querySelectorAll('input[id^="chk_"]');

            checkboxes.forEach(function (checkbox) {
                // Check if the checkbox value (permission code) exists in selectedPermissions array
                if (selectedPermissions.includes(checkbox.value)) {
                    checkbox.checked = true;  // Check the checkbox
                } else {
                    checkbox.checked = false; // Uncheck the checkbox
                }
            });
        };

        const UncheckAll = () => {
            // Find all checkboxes with id starting with 'chk_' and uncheck them
            var checkboxes = document.querySelectorAll('input[id^="chk_"]');
            checkboxes.forEach(function (checkbox) {
                checkbox.checked = false;
            });

            // Optionally clear selectedPermissions array if needed
            selectedPermissions = [];
        };
        const GetCheckedValues = () => {
            // Find all checkboxes that are checked and have an id starting with 'chk_'
            var checkboxes = document.querySelectorAll('input[id^="chk_"]:checked');

            // Map over the checkboxes to get their values
            var checkedValues = Array.from(checkboxes).map(function (checkbox) {
                return checkbox.value;  // Get the value of the checked checkbox
            });

            // Concatenate the values using a '|' delimiter
            concatenatedValues = checkedValues.join('|');


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
            parentIds,
            modulePermissions,
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
 