try {
    var dropdownMenus = document.querySelectorAll(".dropdown-menu.stop");
    dropdownMenus.forEach(function (e) {
        e.addEventListener("click", function (e) {
            e.stopPropagation();
        });
    });
} catch (e) { }
try {
    lucide.createIcons();
} catch (e) { }
//Ramon JS Start
try {
    const themeColorToggle = document.getElementById("light-dark-mode");

    const getSweetAlertThemeOptions = () => {
        const isDark = document.documentElement.getAttribute("data-bs-theme") === "dark";
        return {
            background: isDark ? "#1e1e2f" : "#fff", // SweetAlert2 dark background
            color: isDark ? "#f0f0f0" : "#545454",
            confirmButtonColor: isDark ? "#00c896" : "#4caf50",
        };
    };

    // Patch Swal.fire to inject theme automatically
    const originalSwalFire = Swal.fire;
    Swal.fire = function (options = {}, ...rest) {
        const themedOptions = {
            ...getSweetAlertThemeOptions(),
            ...options, // your options override themed ones
        };
        return originalSwalFire.call(this, themedOptions, ...rest);
    };

    // Theme apply logic
    const applyTheme = (theme) => {
        document.documentElement.setAttribute("data-bs-theme", theme);
        localStorage.setItem("theme", theme);
    };

    // On load
    const savedTheme = localStorage.getItem("theme") || "light";
    applyTheme(savedTheme);

    themeColorToggle?.addEventListener("click", function () {
        const currentTheme = document.documentElement.getAttribute("data-bs-theme");
        const newTheme = currentTheme === "light" ? "dark" : "light";
        applyTheme(newTheme);
    });

} catch (e) {
    console.error("Theme toggle error:", e);
}
//Ramon JS End
try {
    const collapsedToggle = document.querySelector(".mobile-menu-btn");
    const overlay = document.querySelector(".startbar-overlay");
    const sidebar = document.querySelector(".startbar-collapse");
    let originallyExpandedMenus = [];

    const isSidebarCollapsed = () => document.body.getAttribute("data-sidebar-size") === "collapsed";

    const storeExpandedMenus = () => {
        originallyExpandedMenus = [];
        document.querySelectorAll('.navbar-nav .collapse.show').forEach(el => {
            if (el.id) originallyExpandedMenus.push(el.id);
        });
    };

    const collapseMenus = () => {
        originallyExpandedMenus = [];
        document.querySelectorAll('.navbar-nav .collapse.show').forEach(el => {
            if (el.id) {
                originallyExpandedMenus.push(el.id);
                new bootstrap.Collapse(el).hide();
            }
        });
    };

    const expandMenus = () => {
        originallyExpandedMenus.forEach(id => {
            const el = document.getElementById(id);
            if (el && !el.classList.contains("show")) {
                new bootstrap.Collapse(el).show();
            }
        });
    };

    const updateMenuState = () => {
        if (isSidebarCollapsed()) {
            collapseMenus();
        } else {
            expandMenus();
        }
    };

    const toggleSidebar = () => {
        const collapsed = isSidebarCollapsed();
        document.body.setAttribute("data-sidebar-size", collapsed ? "default" : "collapsed");
        updateMenuState();
    };

    collapsedToggle?.addEventListener("click", toggleSidebar);
    overlay?.addEventListener("click", () => {
        document.body.setAttribute("data-sidebar-size", "collapsed");
        updateMenuState();
    });

    const changeSidebarSize = () => {
        if (window.innerWidth >= 310 && window.innerWidth <= 1440) {
            document.body.setAttribute("data-sidebar-size", "collapsed");
        } else {
            document.body.setAttribute("data-sidebar-size", "default");
        }
        updateMenuState();
    };

    // Handle hover (for collapsed sidebar)
    sidebar?.addEventListener("mouseenter", () => {
        if (isSidebarCollapsed()) {
            expandMenus();
        }
    });

    sidebar?.addEventListener("mouseleave", () => {
        if (isSidebarCollapsed()) {
            collapseMenus();
        }
    });

    // On initial load
    window.addEventListener("resize", changeSidebarSize);
    changeSidebarSize();
    storeExpandedMenus(); // capture initial expanded state
} catch (e) {
    console.error("Sidebar menu toggle failed:", e);
}



try {
    const k = document.querySelectorAll('[data-bs-toggle="tooltip"]'),
        l = [...k].map((e) => new bootstrap.Tooltip(e));
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]')),
        popoverList = popoverTriggerList.map(function (e) {
            return new bootstrap.Popover(e);
        });
} catch (e) { }
try {
    changeSidebarSize(),
        window.addEventListener("resize", changeSidebarSize),
        window.addEventListener("resize", () => {
            changeSidebarSize();
        }),
        changeSidebarSize();
} catch (e) { }
function windowScroll() {
    var e = document.getElementById("topbar-custom");
    null != e && (50 <= document.body.scrollTop || 50 <= document.documentElement.scrollTop ? e.classList.add("nav-sticky") : e.classList.remove("nav-sticky"));
}
window.addEventListener("scroll", (e) => {
    e.preventDefault(), windowScroll();
});
const initVerticalMenu = () => {
    var e = document.querySelectorAll(".navbar-nav li .collapse");
    document.querySelectorAll(".navbar-nav li [data-bs-toggle='collapse']").forEach((e) => {
        e.addEventListener("click", function (e) {
            e.preventDefault();
        });
    }),
        e.forEach((e) => {
            e.addEventListener("show.bs.collapse", function (t) {
                //Ramon JS
                //const o = t.target.closest(".collapse.show");
                //document.querySelectorAll(".navbar-nav .collapse.show").forEach((e) => {
                //    e !== t.target && e !== o && new bootstrap.Collapse(e).hide();
                //});

                //Ramon JS Start
                const clickedCollapse = t.target;
                const parent = clickedCollapse.closest('.nav-item');  // Find the parent item of the clicked link

                // Close only siblings at the same level
                if (parent) {
                    const siblings = parent.parentElement.querySelectorAll('.collapse.show');
                    siblings.forEach(sibling => {
                        if (sibling !== clickedCollapse) {
                            new bootstrap.Collapse(sibling).hide();
                        }
                    });
                }
                //Ramon JS End
            });
        }),
        document.querySelector(".navbar-nav") &&
        (document.querySelectorAll(".navbar-nav a").forEach(function (t) {
            var e = window.location.href.split(/[?#]/)[0];
            if (t.href === e) {
                t.classList.add("active"), t.parentNode.classList.add("active");
                let e = t.closest(".collapse");
                for (; e;) e.classList.add("show"), e.parentElement.children[0].classList.add("active"), e.parentElement.children[0].setAttribute("aria-expanded", "true"), (e = e.parentElement.closest(".collapse"));
            }
        }),
            setTimeout(function () {
                var e,
                    a,
                    n,
                    r,
                    c,
                    l,
                    t = document.querySelector(".nav-item li a.active");
                function d() {
                    (e = l += 20), (t = r), (o = c);
                    var e,
                        t,
                        o = (e /= n / 2) < 1 ? (o / 2) * e * e + t : (-o / 2) * (--e * (e - 2) - 1) + t;
                    (a.scrollTop = o), l < n && setTimeout(d, 20);
                }
                null != t && ((e = document.querySelector(".main-nav .simplebar-content-wrapper")), (t = t.offsetTop - 300), e) && 100 < t && ((n = 600), (r = (a = e).scrollTop), (c = t - r), (l = 0), d());
            }, 200));
};
initVerticalMenu();
