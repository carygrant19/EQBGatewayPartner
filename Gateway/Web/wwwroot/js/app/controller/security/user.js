const { createApp, reactive, ref, computed, onMounted, filter, nextTick } = Vue;
const controller = createApp({
    setup() {
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords();
            GetRole();
            GetBranch();
        });
        params.sortColumn = "LastName,FirstName";  
        const roleFormData = reactive({});
        const records = ref([]);
        const roles = ref([]);
        const selectedRoles = ref([]); 
        const branches = ref([]); 
        const isLDAP = ref(false);
        //CRUD

        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            if ($('#searchDescription').val().trim() !== "")
                params.filters.push({ "Property": "FullName", "Value": search.description, "Operator": "Contains" });
            GetRecords();
        };

        const GetRecords = async () => {

            $(".preloader").show();

            const result = await UserService.Search(params);

            if (result.data.totalRecord > 0) {
                records.value = result.data.data;
                show_table.value = true;

                if (params.pageNum > result.data.totalPage) {
                    params.pageNum = params.pageNum - 1;
                    initPages(result.data.totalPage);
                    GetRecords();
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
        const GetBranch = async () => {
            const result = await BranchService.AllMaintenance()

            if (result.data != null) {
                branches.value = result.data;
            }
            else {
                branches.value = [];
            }

        };
        const GetRole = async () => {
            const result = await RoleService.All()

            if (result.data != null) {
                roles.value = result.data.data;
            }
            else {
                roles.value = [];
            }

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
                const result = await UserService.Delete(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "User '" + data.username + "' successfully deleted!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to delete user!"
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
                const result = await UserService.Restore(data.id);
                if (result.data !== '') {
                    if (result.data.status === 'SUCCESS') {
                        swal.fire({
                            text: "User '" + data.username + "' successfully restored!",
                            icon: "success"
                        });

                        GetRecords();
                    }
                    else {
                        swal.fire({
                            icon: 'error',
                            text: "Fail to restore user!"
                        });
                    }
                }
                $('.preloader').fadeOut('slow');
            }

        };

        const Save = async () => {
           
            if ($('#dataForm').parsley().validate()) {
                if (selectedRoles.value.length == 0) {
                    swal.fire({
                        icon: 'error',
                        title: 'Oops...',
                        text: 'You must add roles'
                    });
                    return;
                }
                formData.userRoles = selectedRoles.value;
                $(".preloader").show();
                const result = await UserService.Save(formData);

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
            }
            $('.preloader').fadeOut('slow');
        };

        
        const Sync = async () => {
            if (formData.username.trim() == "") {
                swal.fire({
                    text: "Fill out the USERNAME field!",
                    icon: "error"
                });
                return;
            }
            $(".preloader").show();
            const result = await UserService.CheckAD(formData.username.trim());
            if (result.data != null) {
                if (result.data.username != "") {
                    formData.firstName = result.data.firstName;
                    formData.middleName = result.data.middleName;
                    formData.lastName = result.data.lastName;
                    formData.email = result.data.email;
                    swal.fire({
                        text: "AD User latest details successfully retrieved!",
                        icon: "success"
                    });
                }
                else {
                    formData.firstName = result.data.firstName;
                    formData.middleName = result.data.middleName;
                    formData.lastName = result.data.lastName;
                    formData.email = result.data.email;
                    swal.fire({
                        text: "AD User not exists!",
                        icon: "error"
                    });

                }

            }
            else {

                swal.fire({
                    text: "Error occured!",
                    icon: "error"
                });
            }
            $('.preloader').fadeOut('slow');
        }


        const ResetPassword = (item) => {
            swal.fire({
                title: "Are you sure?",
                text: "This will reset the user's password",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes'
            }).then((result) => {
                if (result.isConfirmed) {
                    UserService.ResetPassword(item.username)
                        .then((result) => {
                            if (result.data.status === 'SUCCESS') {
                                swal.fire({
                                    text: "User password successfully reset!",
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
        const UnlockUser = (item) => {
            swal.fire({
                title: "Are you sure?",
                text: "This will unlock the user access",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes'
            }).then((result) => {
                if (result.isConfirmed) {  
                    UserService.UnlockUser(item.username)
                        .then((result) => {
                            if (result.data.status === 'SUCCESS') {
                                swal.fire({
                                    text: "User access successfully unlocked!",
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
        const SaveRole = () => {
            if ($('#roleForm').parsley().validate()) {
                //$scope.selectedRoles = []
                var roleId = roleFormData.roleId.trim();
                var role = {};
                var selectedRole = {
                    "roleId": '',
                    "roleCode": '',
                    "roleDesc": '',
                };

                for (let x = 0; x < roles.value.length; x++) {
                    if (roleId == roles.value[x].id)
                        role = roles.value[x];
                }

                if (!selectedRoles.value.find(c => c.roleId === roleId)) {
                    selectedRole.roleId = role.id;
                    selectedRole.roleCode = role.code;
                    selectedRole.roleDesc = role.description;
                    selectedRoles.value.push(selectedRole);

                    swal.fire({
                        text: "Role added!",
                        icon: "success"
                    });
                    //$('#roleModal').modal('hide');
                    roleFormData.roleId = '';
                    $('#formModal').modal('show');
                    //formDataMember.member = "";
                    //formDataMember.position = "";
                    //formDataMember.memberChanged = true; 
                } else {
                    swal.fire({
                        text: "Role is already selected!",
                        icon: "error"
                    });
                    $('#formModal').modal('show');
                }
            }
        };
        const RemoveRole = (item) => {
            swal.fire({
                title: "Are you sure?",
                text: "Once delete, this will not be accessible on the system!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            }).then((result) => {
                if (result.isConfirmed) {

                    const index = selectedRoles.value.findIndex(m => m.roleCode === item.roleCode);
                    if (index !== -1) {
                        // If the member is found, remove it
                        selectedRoles.value.splice(index, 1);
                    } else {
                        // Optionally, you can add the member if it's not found
                        // this.selectedMembers.push(member);
                        // this.formDataMember.memberChanged = true;
                    }
                    swal.fire({
                        text: "Role removed!",
                        icon: "success"
                    }); 
                }
            });
        }
        const AddRole = (item) =>
        {
            roleFormData.roleId = '';
        }
        
        const Add = () => {
            $('#dataForm').parsley().reset();
            selectedRoles.value = [];
            disableControl.username = false;
            disableControl.firstName = true;
            disableControl.middleName = true;
            disableControl.lastName = true;
            disableControl.email = true;
            formData.id = '';
            formData.username = '';
            formData.firstName = '';
            formData.middleName = '';
            formData.lastName = '';
            formData.email = '';
            formData.branch = '';
            roleFormData.roleId = '';

            formData.ldapAuthentication = false;
            LDAPChange();
        };

        const Edit = (record) => {
            $('#dataForm').parsley().reset();
            selectedRoles.value = [];
            disableControl.username = true;
            disableControl.firstName = true;
            disableControl.middleName = true;
            disableControl.lastName = true;
            disableControl.email = true; 
            formData.id = record.id;
            formData.username = record.username;
            formData.firstName = record.firstName;
            formData.middleName = record.middleName;
            formData.lastName = record.lastName;
            formData.email = record.email;
            formData.branch = record.branch.id;
            roleFormData.roleId = '';

            formData.ldapAuthentication = record.ldapAuthentication.toString();
            LDAPChange();


            for (let x = 0; x < record.userRoles.length; x++) {
                var selectedRole = {
                    "roleId": record.userRoles[x].roleId,
                    "roleCode": record.userRoles[x].roleCode,
                    "roleDesc": record.userRoles[x].roleDesc,
                };

                selectedRoles.value.push(selectedRole);
            }
        };

      
        const LDAPChange = () =>
        {
            isLDAP.value = formData.ldapAuthentication; 
            disableControl.firstName = (formData.ldapAuthentication === 'true');
            disableControl.middleName = (formData.ldapAuthentication === 'true');
            disableControl.lastName = (formData.ldapAuthentication === 'true');
            disableControl.email = (formData.ldapAuthentication === 'true');
            nextTick();
        }

       
        const returnProps = {
            actionMode,
            pageSizeArray,
            params,
            show_table,
            disableControl,
            search,
            formData,
            roleFormData,
            records,
            roles,
            selectedRoles,
            branches,
            isLDAP,
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
            Sync,
            Save,
            Restore,
            UnlockUser,
            ResetPassword, 
            SaveRole,
            RemoveRole,
            AddRole,
            LDAPChange,
        };
        return {
            ...returnProps,
            ...returnMethod

        };
    }
});
controller.mount('#controller');


