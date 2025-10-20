// ============================================================
// 🌐 GLOBAL TOAST SYSTEM
// A unified feedback system for all controllers, modals, and AJAX flows.
// ============================================================

window.showToast = function (message, type = "success") {
    // Ensure Bootstrap 5 toast element exists
    let container = document.getElementById("global-toast-container");
    if (!container) {
        container = document.createElement("div");
        container.id = "global-toast-container";
        container.className = "position-fixed bottom-0 end-0 p-3";
        container.style.zIndex = 1080;
        document.body.appendChild(container);
    }

    // Map toast type → Bootstrap color class
    const colorMap = {
        success: "text-bg-success",
        error: "text-bg-danger",
        warning: "text-bg-warning text-dark",
        info: "text-bg-info text-dark"
    };
    const colorClass = colorMap[type] || colorMap.success;

    // Create toast element dynamically
    const toast = document.createElement("div");
    toast.className = `toast align-items-center border-0 mb-2 ${colorClass}`;
    toast.setAttribute("role", "alert");
    toast.setAttribute("aria-live", "assertive");
    toast.setAttribute("aria-atomic", "true");

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body fw-semibold">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    container.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
    bsToast.show();

    toast.addEventListener("hidden.bs.toast", () => toast.remove());
};
