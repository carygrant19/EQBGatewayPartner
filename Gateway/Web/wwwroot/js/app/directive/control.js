const formatNumberPlugin = {
    install(app) {
        app.config.globalProperties.$formatNumber = (value) => {
            return new Intl.NumberFormat().format(value);
        };
    }
};

const formatAmountPlugin = {
    install(app) {
        app.config.globalProperties.$formatAmount = (value) => {
            return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
        };
    }
};


const formatDatePlugin = {
    install(app) {
        app.config.globalProperties.$formatDate = (dateString, options = {}) => {
            // Check if dateString is valid
            if (!dateString || isNaN(Date.parse(dateString))) {
                //console.error('Invalid date string:', dateString);
                return '-'; // Or handle it as you wish
            }

            // Convert the date string to a Date object
            const date = new Date(dateString);

            // Default options for date and time formatting
            const defaultOptions = {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit', 
            };

            // Merge default options with any custom options provided
            const formatOptions = { ...defaultOptions, ...options };

            return new Intl.DateTimeFormat('en-US', formatOptions).format(date);
        };
    }
};

const formatDateTextPlugin = {
    install(app) {
        app.config.globalProperties.$formatDateText = (dateString) => {
            // Check if dateString is valid
            if (!dateString || isNaN(Date.parse(dateString))) {
                return '-'; // Handle invalid dates
            }

            // Convert the date string to a Date object
            const date = new Date(dateString);

            // Format options for "Aug 10, 2024" format
            const formatOptions = {
                year: 'numeric',
                month: 'short',
                day: '2-digit'
            };

            // Format the date
            return new Intl.DateTimeFormat('en-US', formatOptions).format(date);
        };
    }
};

const formatDateTimePlugin = {
    install(app) {
        app.config.globalProperties.$formatDateTime = (dateString, options = {}) => {
            // Check if dateString is valid
            if (!dateString || isNaN(Date.parse(dateString))) {
                //console.error('Invalid date string:', dateString);
                return '-'; // Or handle it as you wish
            }

            // Convert the date string to a Date object
            const date = new Date(dateString);

            // Default options for date and time formatting
            const defaultOptions = {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            };

            // Merge default options with any custom options provided
            const formatOptions = { ...defaultOptions, ...options };

            return new Intl.DateTimeFormat('en-US', formatOptions).format(date);
        };
    }
};
const formatTimePlugin = {
    install(app) {
        app.config.globalProperties.$formatTime = (dateString) => {
            // Check if dateString is valid
            if (!dateString || isNaN(Date.parse(dateString))) {
                return '-'; // Handle invalid dates
            }

            // Convert the date string to a Date object
            const date = new Date(dateString);

            // Format options to get only the time
            const timeOptions = {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            };

            return new Intl.DateTimeFormat('en-US', timeOptions).format(date);
        };
    }
};

// Export the plugins
window.formatNumberPlugin = formatNumberPlugin;
window.formatDatePlugin = formatDatePlugin;
window.formatDateTextPlugin = formatDateTextPlugin;
window.formatDateTimePlugin = formatDateTimePlugin;
window.formatTimePlugin = formatTimePlugin;
window.formatAmountPlugin = formatAmountPlugin;