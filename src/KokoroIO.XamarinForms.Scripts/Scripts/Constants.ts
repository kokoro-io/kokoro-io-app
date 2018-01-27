module Messages {
    export const IS_BOTTOM_MARGIN = 15;
    export const IS_TOP_MARGIN = 4;
    export const LOAD_OLDER_MARGIN = 300;
    export const LOAD_NEWER_MARGIN = 300;
    export const HIDE_CONTENT_MARGIN = 60;

    export const LOG_VIEWPORT = false;

    export const IS_DESKTOP = document.documentElement.classList.contains("html-desktop");
    export const IS_TABLET = document.documentElement.classList.contains("html-tablet");
    if (!IS_DESKTOP && !IS_TABLET) {
        document.documentElement.classList.add("html-phone");
    }
}