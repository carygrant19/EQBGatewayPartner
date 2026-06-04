const { createApp, reactive, ref, computed, onMounted, filter } = Vue;

const controller = createApp({
    setup() { 
        onMounted(() => {
            global.GetRecords = GetRecords;
            GetRecords(); 
            GetNews(); 
        });  
        params.sortColumn = "LogDate"; 
        params.descending = true;  
        params.pageSize = 5;
        const mrcTransactionCount = ref([]);   
        const news = ref([]);
         
        const GetNews = async () => {
            const feedUrl = 'https://fintechnews.ph/feed';
            const apiUrl = `https://api.rss2json.com/v1/api.json?rss_url=${encodeURIComponent(feedUrl)}`;
            try {

                const res = await fetch(apiUrl);
                news.value = await res.json();

                //alert(news.items[0].title);

            } catch (err) {
                console.error("Feed error:", err);
                //feed.innerHTML = "<p>Could not load RSS feed.</p>";
            }

        }; 
        const years = ref([]);
        const currentYear = new Date().getFullYear();
        for (let year = currentYear; year >= 2025; year--) {
            years.value.push(year);
        } 
      
        
        const GetRecords = async () => {

            $(".preloader").show();

            const result = await UserService.UserActivityLog(params);
                 
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
 
        const Search = () => {
            params.filters = [];
            params.pageNum = 1;
            //if ($('#searchDescription').val().trim() !== "")
            //    params.filters.push({ "Property": "Description", "Value": search.description, "Operator": "Contains" });
            GetRecords();
        };
         
        //Table Events
        const initPages = (tp) => {
            $('#sync-pagination').twbsPagination('destroy');
            $('#sync-pagination').twbsPagination({
                totalPages: tp,
                initiateStartPageClick: false,
                hideOnlyOnePage: true,
                startPage: params.pageNum,
                onPageClick: (evt, page) => {
                    params.pageNum = page;
                    GetRecords();
                }
            });
        };
        const ItemCountChange = () => {
            params.pageNum = 1;
            Search();
        }; 
          
        const Sort = (col) => {
            params.sortColumn = col;

            if (params.descending) {
                params.descending = false;
            } else {
                params.descending = true;
            }
            GetRecords();
        };
        const SortClass = (col) => {
            if (params.sortColumn === col) {
                if (params.descending) {
                    return 'fa-sort-up';
                } else {
                    return 'fa-sort-down';
                }
            }
            return 'fa fa-sort';
        };
        const ActionModeIcon = () => {

            if (actionMode.value == 'Add') {
                return 'fa-plus-circle';
            }
            else {
                return 'fa-edit';
            }

        }; 
        
        //GetMRCTransactionCount();
        const returnProps = {
            actionMode,  
            pageSizeArray,
            params,
            show_table,
            disableControl, 
            records,  
            branches, 
            months,
            years, 
            news
        };

        // Return methods
        const returnMethod = {
            ItemCountChange: (col) => ItemCountChange(Search),
            Sort: (col) => Sort(col, GetRecords),
            SortClass: (col) => SortClass(col),
            Search,   
            ActionModeIcon, 
        };
        return {
            ...returnProps,
            ...returnMethod

        };
    }
}); 
global.registerFormatPlugins(controller);
controller.mount('#controller');
 