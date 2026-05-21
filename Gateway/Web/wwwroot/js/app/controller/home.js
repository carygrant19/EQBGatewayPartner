"use strict";
// --- 2. Vue Component Logic ---
// Now ref will correctly accept <UserParams>
const { createApp, ref, onMounted } = Vue;
const controller = createApp({
    setup() {
        // State Management
        const params = ref({
            sortColumn: "LogDate",
            descending: true,
            pageSize: 5,
            pageNum: 1,
            filters: []
        });
        const records = ref([]);
        const news = ref([]);
        const years = ref([]);
        const show_table = ref(false);
        const actionMode = ref('Add');
        // Logic: News
        const GetNews = async () => {
            const feedUrl = 'https://fintechnews.ph/feed';
            const apiUrl = `https://api.rss2json.com/v1/api.json?rss_url=${encodeURIComponent(feedUrl)}`;
            try {
                const res = await fetch(apiUrl);
                news.value = await res.json();
            }
            catch (err) {
                console.error("Feed error:", err);
            }
        };
        const GetRecords = async () => {
            $(".preloader").show();
            const result = await UserService.UserActivityLog(params.value);
            if (result.data && result.data.totalRecord > 0) {
                records.value = result.data.data;
                show_table.value = true;
                if (params.value.pageNum > result.data.totalPage) {
                    params.value.pageNum -= 1;
                    initPages(result.data.totalPage);
                    GetRecords();
                }
                else {
                    initPages(result.data.totalPage);
                }
            }
            else {
                records.value = [];
                show_table.value = false;
                $('#sync-pagination').twbsPagination('destroy');
            }
            $('.preloader').fadeOut('slow');
        };
        const Search = () => {
            params.value.filters = [];
            params.value.pageNum = 1;
            GetRecords();
        };
        const initPages = (tp) => {
            $('#sync-pagination').twbsPagination('destroy');
            $('#sync-pagination').twbsPagination({
                totalPages: tp,
                initiateStartPageClick: false,
                hideOnlyOnePage: true,
                startPage: params.value.pageNum,
                onPageClick: (evt, page) => {
                    params.value.pageNum = page;
                    GetRecords();
                }
            });
        };
        const Sort = (col) => {
            params.value.sortColumn = col;
            params.value.descending = !params.value.descending;
            GetRecords();
        };
        const SortClass = (col) => {
            if (params.value.sortColumn === col) {
                return params.value.descending ? 'fa-sort-up' : 'fa-sort-down';
            }
            return 'fa fa-sort';
        };
        onMounted(() => {
            const currentYear = new Date().getFullYear();
            for (let year = currentYear; year >= 2025; year--) {
                years.value.push(year);
            }
            global.GetRecords = GetRecords;
            GetRecords();
            GetNews();
        });
        return {
            params,
            records,
            news,
            years,
            show_table,
            actionMode,
            Search,
            Sort,
            SortClass,
            ItemCountChange: () => Search(),
            ActionModeIcon: () => actionMode.value === 'Add' ? 'fa-plus-circle' : 'fa-edit'
        };
    }
});
global.registerFormatPlugins(controller);
controller.mount('#controller');
//# sourceMappingURL=home.js.map