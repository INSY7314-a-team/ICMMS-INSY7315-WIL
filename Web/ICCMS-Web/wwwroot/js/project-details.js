// ============================================================
//  project-details.js
//  Handles AJAX for Phases & Tasks (Add/Edit/Delete)
//  Project Manager UX for ProjectDetails.cshtml
// ============================================================

document.addEventListener("DOMContentLoaded", () => {
    console.log("🧩 project-details.js loaded and ready.");

    // === Cached DOM elements ===
    const projectContainer = document.getElementById("projectDetailsContainer");
    const projectId = projectContainer?.dataset.projectId;
    const btnAddPhase = document.getElementById("btnAddPhase");
    const phaseModal = new bootstrap.Modal(document.getElementById("phaseModal"));
    const taskModal = new bootstrap.Modal(document.getElementById("taskModal"));

    // --- Guard check ---
    if (!projectId) {
        console.error("❌ No projectId found in dataset. Cannot continue.");
        return;
    }

    console.log(`📁 Managing project: ${projectId}`);

        // ============================================================
        //  UTIL: Fetch contractors for assignment dropdown (DEBUG MODE)
        // ============================================================
        async function loadContractors() {
            try {
                console.log("👷 [loadContractors] Fetching contractors...");
                const res = await fetch("/Projects/GetContractors");
                if (!res.ok) throw new Error(`Failed (${res.status})`);

                const data = await res.json();
                console.log("📦 Contractors:", data);

                const select = document.getElementById("taskAssignedTo");
                select.innerHTML = "<option value=''>Select a contractor...</option>";

                data.forEach(c => {
                    const opt = document.createElement("option");
                    opt.value = c.userId || c.contractorId || "";

                    const company = c.companyName || c.fullName || "Unnamed Contractor";
                    const spec = c.specialization ? ` (${c.specialization})` : "";
                    opt.textContent = `${company}${spec}`;

                    select.appendChild(opt);
                });
            } catch (err) {
                console.error("🔥 loadContractors failed:", err);
            }
        }



    // ============================================================
    //  UTIL: Refresh Phases & Tasks (AJAX reload of the section)
    // ============================================================
    async function refreshPhases() {
        try {
            console.log("🔄 Refreshing phases/tasks...");
            const res = await fetch(`/Projects/Details/${projectId}`, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const html = await res.text();

            // Replace only the Phases tab
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");
            const newSection = doc.querySelector("#phasesContainer");
            const oldSection = document.querySelector("#phasesContainer");

            if (newSection && oldSection) {
                oldSection.replaceWith(newSection);
                console.log("✅ Phases & tasks reloaded successfully.");
                bindDynamicButtons(); // Rebind buttons
            }
        } catch (err) {
            console.error("🔥 refreshPhases() failed:", err);
        }
    }

    // ============================================================
    //  PHASE: Add/Edit
    // ============================================================
    if (btnAddPhase) {
        btnAddPhase.addEventListener("click", () => {
            console.log("➕ Opening Add Phase modal");
            resetPhaseForm();
            document.getElementById("phaseModalLabel").textContent = "Add Phase";
            phaseModal.show();
        });
    }

    function resetPhaseForm() {
        ["phaseId", "phaseName", "phaseDescription", "phaseBudget"].forEach(id => {
            document.getElementById(id).value = "";
        });
        document.getElementById("phaseStatus").value = "Draft";
    }

    document.getElementById("btnSavePhase").addEventListener("click", async () => {
        console.log("💾 Saving phase...");
        const payload = {
            phaseId: document.getElementById("phaseId").value || crypto.randomUUID(),
            projectId: projectId,
            name: document.getElementById("phaseName").value.trim(),
            description: document.getElementById("phaseDescription").value.trim(),
            budget: parseFloat(document.getElementById("phaseBudget").value || 0),
            status: document.getElementById("phaseStatus").value
        };

        const isEdit = !!document.getElementById("phaseId").value;
        const url = isEdit ? "/Projects/UpdatePhase" : "/Projects/AddPhase";
        const method = isEdit ? "PUT" : "POST";

        try {
            const res = await fetch(url, {
                method,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            if (!res.ok) throw new Error(`Phase save failed (${res.status})`);
            console.log("✅ Phase saved successfully!");
            phaseModal.hide();
            await refreshPhases();
        } catch (err) {
            console.error("❌ Failed to save phase:", err);
        }
    });

    // ============================================================
    //  TASK: Add/Edit
    // ============================================================
    async function openAddTaskModal(phaseId) {
        console.log(`➕ Opening Add Task modal for Phase: ${phaseId}`);
        document.getElementById("taskModalLabel").textContent = "Add Task";
        resetTaskForm();
        document.getElementById("taskPhaseId").value = phaseId;
        await loadContractors();
        taskModal.show();
    }

    async function openEditTaskModal(taskId) {
        console.log(`✏️ Opening Edit Task modal for Task: ${taskId}`);
        await loadContractors();

        // Extract task info directly from DOM
        const item = document.querySelector(`[data-task-id='${taskId}']`);
        const name = item.querySelector("strong")?.textContent || "";
        const desc = item.querySelector(".small.text-muted")?.textContent || "";

        document.getElementById("taskModalLabel").textContent = "Edit Task";
        document.getElementById("taskId").value = taskId;
        document.getElementById("taskName").value = name;
        document.getElementById("taskDescription").value = desc;
        taskModal.show();
    }

    function resetTaskForm() {
        ["taskId", "taskName", "taskDescription"].forEach(id => {
            document.getElementById(id).value = "";
        });
        document.getElementById("taskStatus").value = "Pending";
        document.getElementById("taskPriority").value = "Medium";
    }

    document.getElementById("btnSaveTask").addEventListener("click", async () => {
        console.log("💾 Saving task...");
        const payload = {
            taskId: document.getElementById("taskId").value || crypto.randomUUID(),
            projectId: projectId,
            phaseId: document.getElementById("taskPhaseId").value,
            name: document.getElementById("taskName").value.trim(),
            description: document.getElementById("taskDescription").value.trim(),
            assignedTo: document.getElementById("taskAssignedTo").value,
            priority: document.getElementById("taskPriority").value,
            status: document.getElementById("taskStatus").value
        };

        const isEdit = !!document.getElementById("taskId").value;
        const url = isEdit ? "/Projects/UpdateTask" : "/Projects/AddTask";
        const method = isEdit ? "PUT" : "POST";

        try {
            const res = await fetch(url, {
                method,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            if (!res.ok) throw new Error(`Task save failed (${res.status})`);
            console.log("✅ Task saved successfully!");
            taskModal.hide();
            await refreshPhases();
        } catch (err) {
            console.error("❌ Failed to save task:", err);
        }
    });

    // ============================================================
    //  DELETE HANDLERS
    // ============================================================
    async function deletePhase(phaseId) {
        if (!confirm("Delete this phase and its tasks?")) return;
        console.log(`🗑️ Deleting phase ${phaseId}...`);
        try {
            const res = await fetch(`/Projects/DeletePhase/${phaseId}`, { method: "DELETE" });
            if (!res.ok) throw new Error(`Delete failed (${res.status})`);
            console.log("✅ Phase deleted.");
            await refreshPhases();
        } catch (err) {
            console.error("🔥 Failed to delete phase:", err);
        }
    }

    async function deleteTask(taskId) {
        if (!confirm("Delete this task?")) return;
        console.log(`🗑️ Deleting task ${taskId}...`);
        try {
            const res = await fetch(`/Projects/DeleteTask/${taskId}`, { method: "DELETE" });
            if (!res.ok) throw new Error(`Delete failed (${res.status})`);
            console.log("✅ Task deleted.");
            await refreshPhases();
        } catch (err) {
            console.error("🔥 Failed to delete task:", err);
        }
    }

    // ============================================================
    //  DYNAMIC BUTTON BINDINGS
    // ============================================================

            function bindDynamicButtons() {
            console.log("🔁 Binding dynamic buttons (delegated)...");

            // ADD PHASE
            $(document).on("click", "#btnAddPhase", function () {
                console.log("🟡 Click → Add Phase");
                resetPhaseForm();
                document.getElementById("phaseModalLabel").textContent = "Add Phase";
                phaseModal.show();
            });

            // ADD TASK
            $(document).on("click", ".btnAddTask", function () {
                const phaseId = $(this).data("phase-id");
                console.log("🟢 Click → Add Task for Phase", phaseId);
                openAddTaskModal(phaseId);
            });

            // EDIT PHASE
            $(document).on("click", ".btnEditPhase", function () {
                const card = $(this).closest(".phase-card");
                console.log("✏️ Click → Edit Phase", card.data("phase-id"));
                document.getElementById("phaseModalLabel").textContent = "Edit Phase";
                document.getElementById("phaseId").value = card.data("phase-id");
                document.getElementById("phaseName").value = card.find(".fw-bold").text();
                document.getElementById("phaseDescription").value = "";
                document.getElementById("phaseBudget").value = "";
                document.getElementById("phaseStatus").value = "In Progress";
                phaseModal.show();
            });

            // DELETE PHASE
            $(document).on("click", ".btnDeletePhase", function () {
                const id = $(this).data("phase-id");
                console.log("🗑️ Click → Delete Phase", id);
                deletePhase(id);
            });

            // EDIT TASK
            $(document).on("click", ".btnEditTask", function () {
                const id = $(this).data("task-id");
                console.log("✏️ Click → Edit Task", id);
                openEditTaskModal(id);
            });

            // DELETE TASK
            $(document).on("click", ".btnDeleteTask", function () {
                const id = $(this).data("task-id");
                console.log("🗑️ Click → Delete Task", id);
                deleteTask(id);
            });
        }


    // === Initial bindings ===
    bindDynamicButtons();
});
