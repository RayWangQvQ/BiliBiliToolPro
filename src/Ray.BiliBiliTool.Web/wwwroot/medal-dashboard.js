window.biliTool = window.biliTool || {};
window.biliTool.captureMedalPagePosition = function (dashboard) {
    if (!dashboard || !dashboard.isConnected) return null;
    var pagination = dashboard.querySelector(".medal-pagination");
    if (!pagination) return null;
    return (pagination.querySelector(".mud-pagination") || pagination).getBoundingClientRect().top;
};
window.biliTool.restoreMedalPagePosition = function (dashboard, top) {
    if (!dashboard || !dashboard.isConnected || !Number.isFinite(top)) return;
    var pagination = dashboard.querySelector(".medal-pagination");
    if (!pagination) return;
    var controls = pagination.querySelector(".mud-pagination") || pagination;
    if (!pagination.contains(document.activeElement) || document.activeElement.disabled) {
        (pagination.querySelector('button[aria-current="page"]') || pagination).focus({ preventScroll: true });
    }
    // Compensate within nested scrolling panels before scrolling the document.
    for (var parent = pagination.parentElement; parent; parent = parent.parentElement) {
        if (parent === document.body || parent === document.documentElement) break;
        if (/auto|scroll|overlay/.test(getComputedStyle(parent).overflowY)) {
            parent.scrollBy({ top: controls.getBoundingClientRect().top - top, behavior: "instant" });
        }
    }
    window.scrollBy({ top: controls.getBoundingClientRect().top - top, behavior: "instant" });
    var missingSpace = top - controls.getBoundingClientRect().top;
    var grid = dashboard.querySelector(".medal-card-grid");
    // Keep controls in place when a short page reaches the upper scroll limit.
    if (grid && missingSpace > 1) {
        grid.style.minHeight = grid.getBoundingClientRect().height + missingSpace + "px";
        window.scrollBy({ top: controls.getBoundingClientRect().top - top, behavior: "instant" });
    }
};
window.biliTool.resetMedalPagePosition = function (dashboard) {
    var grid = dashboard && dashboard.querySelector(".medal-card-grid");
    if (grid) grid.style.minHeight = "";
};

window.biliTool.focusRecoveryProgress = function (panel) {
    if (!panel || !panel.isConnected) return;
    panel.focus({ preventScroll: true });
    panel.scrollIntoView({ block: "start", behavior: "smooth" });
};
