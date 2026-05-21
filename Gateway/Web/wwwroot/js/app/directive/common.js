// Set up alias for window
global = window;

//Properties
// Global reactive state
global.params = Vue.reactive({
    pageNum: 1,
    pageSize: 10,
    sortColumn: '',
    descending: false,
    filters: [],
});

global.actionMode = Vue.ref('');
global.approvals = Vue.ref([]);
global.approvalStatus = Vue.ref(false);
global.branches = Vue.ref([]); 
global.data = Vue.ref({});
global.disableControl = Vue.reactive({});

global.errors = Vue.ref([]);
global.formData = Vue.reactive({});
global.search = Vue.reactive({});
global.download = Vue.reactive({});
global.show_table = Vue.ref(false); 
global.showControl = Vue.reactive({}); 
global.pageSizeArray = Vue.ref(global.pageSizes || []); 
global.records = Vue.ref([]);
global.statuses = Vue.ref(['PENDING', 'APPROVED', 'PROCESSING', 'PROCESSED', 'CANCELLED']);
global.users = Vue.ref([]);
//



// Global utility methods
global.FormatInputDecimal = (object, prop) => {
    if (!object || !prop) return;

    let val = object[prop];
    if (val === null || val === undefined) return;

    // Convert to string
    val = val.toString();

    // Remove invalid characters (except digits and dot)
    val = val.replace(/[^0-9.]/g, "");

    // Prevent multiple decimals
    val = val.replace(/(\..*?)\..*/g, "$1");

    // Split decimal safely
    let [intPart, decPart] = val.split(".");

    // Remove leading zeros unless value is "0.x"
    intPart = intPart.replace(/^0+(?!$)/, "");

    // Format integer part with commas
    if (intPart) {
        intPart = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    }

    // Handle decimals
    if (decPart !== undefined) {
        decPart = decPart.substring(0, 2); // max 2 decimals
        object[prop] = intPart + "." + decPart;
    } else {
        object[prop] = intPart;
    }
};



global.ActionModeIcon = () => {
    return global.actionMode.value === 'Add' ? 'fa-plus-circle' : 'fa-edit';
};

global.SortClass = (col, params = global.params) => {
    if (params.sortColumn === col) {
        return params.descending ? 'fa-sort-down' : 'fa-sort-up';
    }
    return 'fa fa-sort';
};

global.Sort = async (col, getRecordsCallback = global.GetRecords, params = global.params) => {
    if (params.sortColumn === col) {
        params.descending = !params.descending;
    } else {
        params.sortColumn = col;
        params.descending = false;
    }

    if (typeof getRecordsCallback === "function") {
        const result = getRecordsCallback();
        if (result instanceof Promise) await result;
    }
};
global.ItemCountChange = (params = global.params, callback = global.GetRecords, ...callbackArgs) => {
    params.pageNum = 1;

    if (typeof callback === 'function') {
        callback(...callbackArgs);
    } 
};
global.initializeDatepickerForm = (selector, data, modelKey, dateFormat, setCurrentDate = false) => { 
    
    if ($(selector).data('airdatepicker')) {
        $(selector).data('airdatepicker').destroy();
    }

    const datepicker = new AirDatepicker(selector, {
        buttons: ['clear'],
        language: 'en',
        timepicker: false,
        dateFormat: dateFormat,
        range: false,
        multipleDates: false,
        onSelect({ formattedDate }) {
            data[modelKey] = formattedDate;
            const input = document.querySelector(selector);
            if (input) input.value = formattedDate;  
        }
    });

    
    let dateToSet = null;

    if (data[modelKey]) {
        const parsed = new Date(data[modelKey]);
        if (!isNaN(parsed)) {
            dateToSet = parsed;
        }
    }

    if (!dateToSet && setCurrentDate) {
        dateToSet = new Date();
        data[modelKey] = dateToSet.toISOString().split('T')[0];
    }

     
    if (dateToSet) {
        datepicker.selectDate(dateToSet);

         
        setTimeout(() => {
            const input = document.querySelector(selector);
            if (input) {
                input.value = datepicker.formatDate(dateToSet, dateFormat);
            }
        }, 50);
    }
};
//Records paging
global.initPages = function (totalPages, getRecordsCallback = global.GetRecords, ...callbackArgs) {
    $('#sync-pagination').twbsPagination('destroy'); 
    $('#sync-pagination').twbsPagination({
        totalPages,
        initiateStartPageClick: false,
        hideOnlyOnePage: true,
        startPage: global.params.pageNum,
        onPageClick: (evt, page) => {
            global.params.pageNum = page;

            if (typeof getRecordsCallback === "function") {
                const result = getRecordsCallback(...callbackArgs);
                if (result instanceof Promise) {
                    //result.catch(console.error);
                }
            }
        }
    });
};
global.Reset = (search) => {
    Object.keys(search).forEach(key => {
        search[key] = '';
    });
}
//Design
global.getBadgeClass = (status) => {
    const badgeColors = {
        PENDING: 'bg-warning',
        APPROVED: 'bg-primary',
        PROCESSED: 'bg-success',
        PROCESSING: 'bg-info',
        CANCELLED: 'bg-danger',

        TRANSFER: 'bg-info',
        FAILED: 'bg-danger',
        SUCCESS: 'bg-success',
        ERROR: 'bg-danger',
    };
    return badgeColors[status] || ''; // Default to empty string if status not found
};

//Months
global.months = Vue.ref([
    { value: '01', name: 'January' },
    { value: '02', name: 'February' },
    { value: '03', name: 'March' },
    { value: '04', name: 'April' },
    { value: '05', name: 'May' },
    { value: '06', name: 'June' },
    { value: '07', name: 'July' },
    { value: '08', name: 'August' },
    { value: '09', name: 'September' },
    { value: '10', name: 'October' },
    { value: '11', name: 'November' },
    { value: '12', name: 'December' }
]);


global.registerFormatPlugins = (app) => {
    app.use(window.formatNumberPlugin);
    app.use(window.formatAmountPlugin);
    app.use(window.formatDatePlugin);
    app.use(window.formatDateTextPlugin);
    app.use(window.formatDateTimePlugin);
    app.use(window.formatTimePlugin);
};