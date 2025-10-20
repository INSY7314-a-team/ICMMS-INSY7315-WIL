// ============================================================
// setup-project.js — AJAX + Toast Integration
// For SetupProject.cshtml
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    console.log("🧩 setup-project.js loaded");

    // --------------- Helper: AJAX POST ---------------
    async function postForm(url, formData) {
        const response = await fetch(url, {
            method: "POST",
            body: formData,
        });
        return await response.json().catch(() => ({}));
    }

    // --------------- ADD PHASE ---------------
    const phaseForm = document.querySelector("form[asp-action='AddPhase']");
    if (phaseForm) {
        phaseForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const formData = new FormData(phaseForm);
            console.log("📦 Adding Phase...");
            const res = await postForm(phaseForm.action, formData);

            if (res.success) {
                showToast("success", res.message || "Phase added!");
                setTimeout(() => location.reload(), 800);
            } else {
                showToast("error", res.message || "Failed to add phase.");
            }
        });
    }

    // --------------- ADD TASK ---------------
    const taskForm = document.querySelector("form[asp-action='AddTask']");
    if (taskForm) {
        taskForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const formData = new FormData(taskForm);
            console.log("🧱 Adding Task...");
            const res = await postForm(taskForm.action, formData);

            if (res.success) {
                showToast("success", res.message || "Task added!");
                setTimeout(() => location.reload(), 800);
            } else {
                showToast("error", res.message || "Failed to add task.");
            }
        });
    }

    // --------------- FINALIZE PROJECT ---------------
    const finalizeBtn = document.querySelector("button[data-action='finalize']");
    if (finalizeBtn) {
        finalizeBtn.addEventListener("click", async (e) => {
            e.preventDefault();
            const projectId = finalizeBtn.dataset.projectId;
            if (!confirm("Finalize project setup?")) return;

            const formData = new FormData();
            formData.append("projectId", projectId);
            const res = await postForm("/ProjectManager/FinalizeProject", formData);

            if (res.success) {
                showToast("success", res.message);
                setTimeout(() => (window.location.href = "/ProjectManager/Dashboard"), 1000);
            } else {
                showToast("error", res.message);
            }
        });
    }

    // --------------- COMPLETE PROJECT ---------------
    const completeBtn = document.querySelector("button[data-action='complete']");
    if (completeBtn) {
        completeBtn.addEventListener("click", async (e) => {
            e.preventDefault();
            const projectId = completeBtn.dataset.projectId;
            if (!confirm("Mark this project as completed?")) return;

            const formData = new FormData();
            formData.append("projectId", projectId);
            const res = await postForm("/ProjectManager/CompleteProject", formData);

            if (res.success) {
                showToast("success", res.message);
                setTimeout(() => (window.location.href = "/ProjectManager/Dashboard"), 1000);
            } else {
                showToast("error", res.message);
            }
        });
    }
});
