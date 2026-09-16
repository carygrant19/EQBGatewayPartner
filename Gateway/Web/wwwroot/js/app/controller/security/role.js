const { createApp, reactive, ref, computed, onMounted, filter } = Vue;
const controller = createApp({
    setup() {
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
            GetAllPermissions();
            GetModuleGroup();
        }); 
      
        params.sortColumn = "Description"; 

       
        var ctrAccess = 0;
        var ctrModule = 0;
        var modules = [];
        var permissions = [];
        var roleModules = [];
        const moduleGroups = ref([]);

        //CRUD
        const GetModuleGroup = async () => {
            const result = await ModuleService.ModuleGroup();
            moduleGroups.value = result.data; 
        }
        const GetRoleModuleById = async (roleId) => {
            const result = await RoleService.ById(roleId);
            roleModules = result.data;
            ctrAccess = roleModules.length;

            roleModules.map((value, key) => {


                $("input:checkbox[id='chk_" + value.id + "']").prop('checked', true);
                $("#chk_" + value.id + "").prop('checked', true);
                //$(".chkPermission_" + value.id).prop('disabled', false);
                if (value.modulePermission.trim() != '') {
                    var modulePermission = value.modulePermission.split('|');
                    modulePermission.forEach((perm) => {
                        $('#chkPermission_' + value.id + '_' + perm).prop('checked', true);
                        ctrAccess += 1;
                    });
                }

                //$('#chkPermission_' + value.id+ '_').each(function (index, obj) {
                //    if (value.action.includes(this.value)) {
                //        $(this).prop('checked', true);
                //    }
                //});
            });

            if (ctrAccess > 0)
                $('#chk_0').prop('checked', true);
            else
                $('#chk_0').prop('checked', false); 
        }
        const GetRecords = async () => {

            $(".preloader").show();

            const result = await RoleService.Search(params);

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
                const result = await RoleService.Delete(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Role '" + data.code + "' successfully deleted!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to delete role!"
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
                const result = await RoleService.Restore(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "Role '" + data.code + "' successfully restored!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to restore role!"
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }

        };

        const Save = async () => {
            if ($('#dataForm').parsley().validate()) {
                $(".preloader").show();
                var checkedData = GetAllChecked();
                formData.roleModulePermission = checkedData;

                const result = await RoleService.Save(formData);

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
        const ModuleGroupChange = async () => {
            const selectedGroup = search.moduleGroup?.trim().toUpperCase();

            // ✅ Hide/show the header checkbox depending on filter
            if (!selectedGroup || selectedGroup === "") {
                $("#chk_0").show();  // Show when "All" is selected
            } else {
                $("#chk_0").hide();  // Hide when specific group selected
            }

            $("#modulesTable tbody tr").each(function () {
                const row = $(this);
                const rowId = (row.attr("id") || "").toUpperCase();
                const hiddenGroup = row.find("td:first").text().trim().toUpperCase();

                // ✅ Always show HOME row — by ID or text
                if (rowId.includes("HOME_MODULE") || hiddenGroup === "HOME") {
                    row.show();
                    return;
                }

                // ✅ Show all if no filter
                if (!selectedGroup || selectedGroup === "") {
                    row.show();
                }
                // ✅ Show only rows matching selected group
                else if (hiddenGroup === selectedGroup) {
                    row.show();
                }
                // 🔴 Hide others
                else {
                    row.hide();
                }
            });
        };




        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            if ($('#searchDescription').val().trim() !== "")
                params.filters.push({ "Property": "Description", "Value": search.description, "Operator": "Contains" });
            GetRecords();
        };
        const Add = async () => {
            $('#dataForm').parsley().reset();
            disableControl.code = false;
            formData.id = '';
            formData.code = '';
            formData.description = '';
            uncheckAllCheckboxes();
            search.moduleGroup = '';
            await ModuleGroupChange();
        };

        const Edit = async (record) => {
            $('#dataForm').parsley().reset();
            disableControl.code = true;
            formData.id = record.id;
            formData.code = record.code;
            formData.description = record.description;
            uncheckAllCheckboxes();
            GetRoleModuleById(record.id);
            search.moduleGroup = '';
            await ModuleGroupChange();
        }; 
       
        const clearTableRows = () => {
            const table = document.getElementById('modulesTable');
            const rows = table.getElementsByTagName('tr');

            // Skip the first row (header row)
            while (rows.length > 1) {
                table.deleteRow(1);
            }
        }
        const GetModules = async () => {
            const result = await ModuleService.AllParent();

            if (result.data != null) {
                modules = result.data;
                ctrModule = modules.length;

                modules.map((value, key) => {
                    // Construct unique hidden ID
                    const hiddenId = `${value.moduleGroup}`;

                    $("<tr></tr>")
                        .addClass("treegrid-" + value.id + (value.parentId != 0 ? " treegrid-parent-" + value.parentId : ""))
                        .appendTo($('#modulesTable'))
                        .html(
                            // 🟢 Hidden columns
                            `<td id="${hiddenId}" hidden>${value.moduleGroup}</td>` +
                            `<td hidden>${value.id}</td>` +

                            // 🟢 Visible columns
                            `<td>${value.moduleGroupDesc}</td>` +
                            `<td>${value.name}${value.show == false ? " <i class='fas fa-eye-slash'></i>" : ""}</td>` +
                            `<td class='text-center'>
                        <input type='checkbox' value='${value.id}' class='chk_${value.parentId}' id='chk_${value.id}' />
                    </td>` +
                            `<td id='tdAction${value.id}'>
                        ${FormatPermission(value.id, value.permissions)}
                    </td>`
                        );
                });
            } else {
                modules.value = [];
            }

            AttachCheckBoxListener();
        };


        const FormatPermission = (id, permissions, isDisabled = false) => {
            var html = "";

            if (permissions && permissions !== '') {
                permissions.split('|').map((value) => {
                    const disabledAttr = isDisabled ? ' disabled' : '';
                    // keep permission checkboxes unchecked but disabled for HOME
                    html += `<input type='checkbox' id='chkPermission_${id}_${value}' value='${value}' class='chk_${id}'${disabledAttr}/>`
                        + ` <label style='margin-right:20px;' class='font-weight-light'> ${GetPermissionName(value)}</label>`;
                });
            }

            return html;
        };
        const GetPermissionName = (code) => {

            var name = '';

            for (var i = 0; i < permissions.length; i++) {
                if (code === permissions[i].id) {
                    return permissions[i].description;
                }
            };

            return name;
        };
        const GetAllPermissions = async () => {
            const result = await PermissionService.All();
            if (result.data !== null) {
                permissions = result.data;

                GetModules();
            }

        };
        const GetAllChecked = () => {
            let selectedData = [];

            // Iterate through each module checkbox that starts with 'chk_' and isn't 'chk_0'
            $('input[type="checkbox"]').each(function () {
                let moduleCheckbox = $(this);

                // Check if the checkbox is not 'chk_0', starts with 'chk_', and is checked
                if (moduleCheckbox.attr('id') !== 'chk_0' && moduleCheckbox.attr('id').startsWith('chk_') && moduleCheckbox.is(':checked')) {
                    let moduleId = moduleCheckbox.val(); // Get the module code from the checkbox value
                    let parentRow = moduleCheckbox.closest('tr'); // Find the closest parent row
                    let permissionCheckboxes = parentRow.find(`input[type="checkbox"].chk_${moduleCheckbox.attr('id').split('_')[1]}`);
                    let permissions = [];

                    // Iterate through permission checkboxes to see if any are checked
                    permissionCheckboxes.each(function () {
                        if ($(this).is(':checked')) {
                            permissions.push($(this).val());
                        }
                    });

                    // Push the module with the collected permissions or null if no permissions are selected
                    selectedData.push({
                        ModuleId: moduleId,
                        PermissionId: permissions.length > 0 ? permissions : null
                    });
                }
            });

            return selectedData;


        }
        uncheckAllCheckboxes = () => {
            const checkboxes = document.querySelectorAll('input[type="checkbox"]');
            checkboxes.forEach(checkbox => {
                checkbox.checked = false;
            });
        };
        const AttachCheckBoxListener = () => {
            function toggleChildCheckboxes(parentCheckbox, checked) {
                const childCheckboxes = document.querySelectorAll(`input[type="checkbox"].${parentCheckbox.id}`);
                childCheckboxes.forEach(checkbox => {
                    checkbox.checked = checked;
                    // Recurse to handle nested children
                    toggleChildCheckboxes(checkbox, checked);
                });
            }

            // Function to update the parent checkbox based on child checkboxes
            function updateParentCheckboxes(parentId) {
                const parentCheckbox = document.getElementById(parentId);
                if (parentCheckbox) {
                    // Get all child checkboxes with the parent ID as class
                    const childCheckboxes = document.querySelectorAll(`input[type="checkbox"].${parentId}`);
                    const allChecked = Array.from(childCheckboxes).every(checkbox => checkbox.checked);
                    const anyChecked = Array.from(childCheckboxes).some(checkbox => checkbox.checked);

                    // Update parent checkbox based on child checkboxes
                    parentCheckbox.checked = anyChecked;

                    // If there is an ancestor, update it
                    const parentClasses = parentCheckbox.className.split(' ').filter(cls => cls !== parentId);
                    parentClasses.forEach(parentClass => {
                        updateParentCheckboxes(parentClass);
                    });
                }
            }

            // Function to handle checkbox changes
            function handleCheckboxChange(event) {
                const checkbox = event.target;
                if (checkbox.id) {
                    // Toggle all child checkboxes when a parent checkbox is changed
                    toggleChildCheckboxes(checkbox, checkbox.checked);
                }
                if (checkbox.className) {
                    // Update parent checkboxes when a child checkbox is changed
                    updateParentCheckboxes(checkbox.className);
                }
            }

            // Attach event listeners to all checkboxes
            document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
                checkbox.addEventListener('change', handleCheckboxChange);
                //console.log('added listener all');
            });

            // Initial update of parent checkboxes
            document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
                if (checkbox.id) {
                    //console.log('added listener child');
                    updateParentCheckboxes(checkbox.id);
                }
            });
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
            moduleGroups,
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
            ModuleGroupChange,
        };
        return {
            ...returnProps,
            ...returnMethod

        };
    }
});
controller.mount('#controller');
