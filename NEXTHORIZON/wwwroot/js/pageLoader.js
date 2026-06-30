// Use logic to show loader during transitions
window.addEventListener("beforeunload", function () {
    document.getElementById("loading-overlay").style.display = "flex";
});

// Hide loader once the new page has fully loaded
window.addEventListener("load", function () {
    document.getElementById("loading-overlay").style.display = "none";
});