 
// This tells TS what Vue's ref and functions are capable of
interface VueStatic {
    createApp: (options: any) => any;
    ref: <T>(value: T) => { value: T };
    onMounted: (callback: () => void) => void;
    reactive: (target: object) => any;
    computed: (getter: () => any) => any;
}

// Replace 'any' with our new 'VueStatic' interface
declare var Vue: VueStatic;
declare var $: any;
declare var HolidayService: any;
declare var UserService: any;
declare var global: any;

// --- 1. Type Definitions (Interfaces) ---
interface NewsItem {
    title: string;
    link: string;
    pubDate: string;
}
 

interface UserParams {
    sortColumn: string;
    descending: boolean;
    pageSize: number;
    pageNum: number;
    filters: any[];
}

// --- 2. Vue Component Logic ---
// Now ref will correctly accept <UserParams>
const { createApp, ref, onMounted } = Vue;

const controller = createApp({
    setup() {
        // State Management
        const params = ref<UserParams>({
            sortColumn: "LogDate",
            descending: true,
            pageSize: 5,
            pageNum: 1,
            filters: []
        });

        const records = ref<any[]>([]); 
        const news = ref<any>([]);
        const years = ref<number[]>([]);
        const show_table = ref(false);
        const actionMode = ref('Add');

        // Logic: News
        const GetNews = async (): Promise<void> => {
            const feedUrl = 'https://fintechnews.ph/feed';
            const apiUrl = `https://api.rss2json.com/v1/api.json?rss_url=${encodeURIComponent(feedUrl)}`;
            try {
                const res = await fetch(apiUrl);
                news.value = await res.json();
            } catch (err) {
                console.error("Feed error:", err);
            }
        }; 

        const GetRecords = async (): Promise<void> => {
            $(".preloader").show();
            const result = await UserService.UserActivityLog(params.value);

            if (result.data && result.data.totalRecord > 0) {
                records.value = result.data.data;
                show_table.value = true;

                if (params.value.pageNum > result.data.totalPage) {
                    params.value.pageNum -= 1;
                    initPages(result.data.totalPage);
                    GetRecords();
                } else {
                    initPages(result.data.totalPage);
                }
            } else {
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

        const initPages = (tp: number) => {
            $('#sync-pagination').twbsPagination('destroy');
            $('#sync-pagination').twbsPagination({
                totalPages: tp,
                initiateStartPageClick: false,
                hideOnlyOnePage: true,
                startPage: params.value.pageNum,
                onPageClick: (evt: any, page: number) => {
                    params.value.pageNum = page;
                    GetRecords();
                }
            });
        };
        interface User {
            id: number;
            name: string;
            email: string;
            role: 'admin' | 'user' | 'guest';
        }
         
        const Sort = (col: string) => {
            params.value.sortColumn = col;
            params.value.descending = !params.value.descending;
            GetRecords();
        };

        const SortClass = (col: string) => {
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