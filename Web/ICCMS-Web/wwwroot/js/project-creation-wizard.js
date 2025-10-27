/*
 * Project Creation Wizard JavaScript
 * Handles the multi-step project creation process
 */

class ProjectCreationWizard {
  constructor() {
    this.currentStep = 1;
    this.totalSteps = 5; // Basic, Budget, Phases, Tasks, Review
    this.formData = {};
    this.clients = [];
    this.contractors = [];
    this.projectId = null; // created on StartDraft
    this.autosaveIntervalId = null;

    this.init();
  }

  init() {
    this.bindEvents();
    this.loadClients();
    this.loadContractors();
    this.setupFormValidation();
    this.setupAutosave();
  }

  bindEvents() {
    // Modal events
    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("show.bs.modal", () => {
        this.resetWizard();
      });

    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("hidden.bs.modal", () => {
        this.resetWizard();
      });

    // Navigation buttons
    document.getElementById("nextStepBtn")?.addEventListener("click", () => {
      this.nextStep();
    });

    document.getElementById("prevStepBtn")?.addEventListener("click", () => {
      this.previousStep();
    });

    document
      .getElementById("createProjectBtn")
      ?.addEventListener("click", () => {
        this.finalizeProject();
      });

    // Form validation on input change
    document
      .querySelectorAll(
        "#projectCreationForm input, #projectCreationForm select, #projectCreationForm textarea"
      )
      .forEach((element) => {
        element.addEventListener("change", () => {
          this.validateCurrentStep();
        });
      });

    // Date validation
    document.getElementById("startDate")?.addEventListener("change", () => {
      this.validateDateRange();
    });

    document
      .getElementById("endDatePlanned")
      ?.addEventListener("change", () => {
        this.validateDateRange();
      });
  }

  async loadClients() {
    try {
      // First try to get clients from the dashboard data
      if (window.dashboardClients && window.dashboardClients.length > 0) {
        this.clients = window.dashboardClients;
        this.populateClientSelect();
        return;
      }

      // Fallback to API call if dashboard data is not available
      const response = await fetch("/ProjectManager/GetClients");
      if (response.ok) {
        this.clients = await response.json();
        this.populateClientSelect();
      } else {
        console.error("Failed to load clients:", response.statusText);
        this.showError("Failed to load clients. Please refresh the page.");
      }
    } catch (error) {
      console.error("Error loading clients:", error);
      this.showError("Error loading clients. Please check your connection.");
    }
  }

  populateClientSelect() {
    const clientSelect = document.getElementById("clientSelect");
    if (!clientSelect) return;

    clientSelect.innerHTML = '<option value="">Select a client...</option>';

    this.clients.forEach((client) => {
      const option = document.createElement("option");
      option.value = client.userId;
      option.textContent = `${client.fullName} (${client.email})`;
      clientSelect.appendChild(option);
    });
  }

  async loadContractors() {
    try {
      const response = await fetch("/ProjectManager/GetContractors");
      if (response.ok) {
        this.contractors = await response.json();
      }
    } catch (e) {
      console.warn("Failed to load contractors", e);
    }
  }

  setupFormValidation() {
    // Bootstrap validation setup
    const form = document.getElementById("projectCreationForm");
    if (form) {
      form.addEventListener("submit", (e) => {
        e.preventDefault();
        e.stopPropagation();
      });
    }
  }

  nextStep() {
    if (this.validateCurrentStep()) {
      this.saveCurrentStepData();

      if (this.currentStep < this.totalSteps) {
        this.currentStep++;
        this.updateWizardDisplay();
      }
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.updateWizardDisplay();
    }
  }

  validateCurrentStep() {
    const currentStepElement = document.getElementById(
      `step${this.currentStep}`
    );
    if (!currentStepElement) return false;

    let isValid = true;
    const requiredFields = currentStepElement.querySelectorAll("[required]");

    requiredFields.forEach((field) => {
      if (!field.value.trim()) {
        field.classList.add("is-invalid");
        isValid = false;
      } else {
        field.classList.remove("is-invalid");
      }
    });

    // Additional validation for specific steps
    if (this.currentStep === 2) {
      isValid = this.validateDateRange() && isValid;
      isValid = this.validateBudget() && isValid;
    }

    return isValid;
  }

  validateDateRange() {
    const startDate = document.getElementById("startDate")?.value;
    const endDate = document.getElementById("endDatePlanned")?.value;

    if (startDate && endDate) {
      const start = new Date(startDate);
      const end = new Date(endDate);

      if (end <= start) {
        document.getElementById("endDatePlanned").classList.add("is-invalid");
        this.showError("End date must be after start date.");
        return false;
      } else {
        document
          .getElementById("endDatePlanned")
          .classList.remove("is-invalid");
        return true;
      }
    }
    return true;
  }

  validateBudget() {
    const budget = document.getElementById("budgetPlanned")?.value;
    if (budget && parseFloat(budget) <= 0) {
      document.getElementById("budgetPlanned").classList.add("is-invalid");
      this.showError("Budget must be greater than 0.");
      return false;
    } else {
      document.getElementById("budgetPlanned").classList.remove("is-invalid");
      return true;
    }
  }

  saveCurrentStepData() {
    const currentStepElement = document.getElementById(
      `step${this.currentStep}`
    );
    if (!currentStepElement) return;

    const formData = new FormData();
    const inputs = currentStepElement.querySelectorAll(
      "input, select, textarea"
    );

    inputs.forEach((input) => {
      if (input.name && input.value) {
        this.formData[input.name] = input.value;
      }
    });
  }

  updateWizardDisplay() {
    // Update progress bar
    const progress = (this.currentStep / this.totalSteps) * 100;
    document.getElementById("wizardProgress").style.width = `${progress}%`;

    // Update step counter
    document.getElementById("currentStep").textContent = this.currentStep;

    // Update step titles
    const stepTitles = [
      "Basic Information",
      "Budget & Timeline",
      "Phases",
      "Tasks & Contractors",
      "Review & Finalize",
    ];
    document.getElementById("stepTitle").textContent =
      stepTitles[this.currentStep - 1];

    // Show/hide steps
    for (let i = 1; i <= this.totalSteps; i++) {
      const stepElement = document.getElementById(`step${i}`);
      if (stepElement) {
        if (i === this.currentStep) {
          stepElement.classList.remove("d-none");
        } else {
          stepElement.classList.add("d-none");
        }
      }
    }

    // Update navigation buttons
    this.updateNavigationButtons();

    // Update review step if we're on step 4
    if (this.currentStep === 5) {
      this.updateReviewStep();
    }
  }

  updateNavigationButtons() {
    const prevBtn = document.getElementById("prevStepBtn");
    const nextBtn = document.getElementById("nextStepBtn");
    const createBtn = document.getElementById("createProjectBtn");

    if (prevBtn) {
      prevBtn.style.display = this.currentStep > 1 ? "inline-block" : "none";
    }

    if (nextBtn && createBtn) {
      if (this.currentStep === this.totalSteps) {
        nextBtn.style.display = "none";
        createBtn.style.display = "inline-block";
      } else {
        nextBtn.style.display = "inline-block";
        createBtn.style.display = "none";
      }
    }
  }

  updateReviewStep() {
    // Populate review fields with collected data
    document.getElementById("reviewName").textContent =
      this.formData.Name || "-";
    document.getElementById("reviewDescription").textContent =
      this.formData.Description || "-";
    document.getElementById("reviewBudget").textContent =
      this.formData.BudgetPlanned || "-";
    document.getElementById("reviewStartDate").textContent =
      this.formData.StartDate || "-";
    document.getElementById("reviewEndDate").textContent =
      this.formData.EndDatePlanned || "-";
    document.getElementById("reviewStatus").textContent =
      this.formData.Status || "-";

    // Find client name
    const clientId = this.formData.ClientId;
    const client = this.clients.find((c) => c.userId === clientId);
    document.getElementById("reviewClient").textContent = client
      ? client.fullName
      : "-";

    // Grouped Phases -> Tasks view
    const phases = this.collectPhases();
    const tasks = this.collectTasks();
    const groupsContainer = document.getElementById("reviewPhaseTaskGroups");
    if (groupsContainer) {
      groupsContainer.innerHTML = "";
      if (phases.length === 0) {
        groupsContainer.innerHTML =
          '<div class="text-muted">No phases added</div>';
      } else {
        const tasksByPhase = tasks.reduce((acc, t) => {
          const key = t.phaseId || "";
          acc[key] = acc[key] || [];
          acc[key].push(t);
          return acc;
        }, {});

        phases.forEach((p, idx) => {
          const card = document.createElement("div");
          card.className = "card mb-2";
          const listItems = (tasksByPhase[p.phaseId] || []).map((t, i) => {
            return `<li class=\"list-group-item\">${i + 1}. ${
              t.name || "Untitled"
            }</li>`;
          });
          const emptyText =
            listItems.length === 0
              ? '<li class="list-group-item text-muted">No tasks</li>'
              : "";
          card.innerHTML = `
            <div class="card-header">
              <strong>${idx + 1}. ${p.name || "Untitled Phase"}</strong>
            </div>
            <ul class="list-group list-group-flush">
              ${listItems.join("") || emptyText}
            </ul>`;
          groupsContainer.appendChild(card);
        });
      }
    }
  }

  async ensureDraft() {
    if (this.projectId) return;
    try {
      const projectData = {
        project: { status: "Draft" },
        phases: [],
        tasks: [],
      };

      const res = await fetch("/ProjectManager/SaveProject", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(projectData),
      });

      const data = await res.json();
      if (data?.success) {
        this.projectId = data.projectId;
        console.log("Draft project created:", this.projectId);
      }
    } catch (e) {
      console.warn("SaveProject failed", e);
    }
  }

  setupAutosave() {
    // Start draft when modal opens
    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("shown.bs.modal", async () => {
        await this.ensureDraft();
      });

    // Autosave every 10s
    this.autosaveIntervalId = setInterval(() => this.autosave(), 10000);

    // Autosave on navigation/unload
    window.addEventListener("beforeunload", () => {
      this.autosave(true);
    });
  }

  async autosave(isSync = false) {
    if (!this.projectId) return;

    const projectData = this.collectProjectData();
    const phases = this.collectPhases().filter((p) => p.name?.trim());

    const request = {
      project: projectData,
      phases: phases,
      tasks: [],
    };

    try {
      await fetch("/ProjectManager/SaveProject", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request),
        keepalive: isSync, // allow send on unload
      });
    } catch (e) {
      if (!isSync) console.debug("Autosave failed", e);
    }
  }

  parseDateInput(value) {
    if (!value) return null;
    const d = new Date(value);
    if (isNaN(d.getTime())) return null;
    return d.toISOString();
  }

  buildPhaseRow(phase = {}) {
    const id = phase.phaseId || "phase_" + crypto.randomUUID();
    const wrapper = document.createElement("div");
    wrapper.className = "card mb-2";
    wrapper.innerHTML = `
      <div class="card-body">
        <div class="row g-2 align-items-end">
          <div class="col-md-4">
            <label class="form-label">Phase Name</label>
            <input type="text" class="form-control phase-name" data-id="${id}" value="${
      phase.name || ""
    }" />
          </div>
          <div class="col-md-4">
            <label class="form-label">Description</label>
            <input type="text" class="form-control phase-desc" value="${
              phase.description || ""
            }" />
          </div>
          <div class="col-md-2">
            <label class="form-label">Budget</label>
            <div class="input-group">
              <span class="input-group-text">R</span>
              <input type="number" class="form-control phase-budget" value="${
                phase.budget || ""
              }" step="0.01" min="0" />
            </div>
          </div>
          <div class="col-md-2 text-end">
            <button type="button" class="btn btn-outline-danger btn-sm remove-phase">Remove</button>
          </div>
        </div>
        <div class="row g-2 mt-2">
          <div class="col-md-6">
            <label class="form-label">Start Date</label>
            <input type="date" class="form-control phase-start" value="${
              phase.startDate ? new Date(phase.startDate).toISOString().split('T')[0] : ""
            }" />
          </div>
          <div class="col-md-6">
            <label class="form-label">End Date</label>
            <input type="date" class="form-control phase-end" value="${
              phase.endDate ? new Date(phase.endDate).toISOString().split('T')[0] : ""
            }" />
          </div>
        </div>
      </div>`;
    wrapper.querySelector(".remove-phase").addEventListener("click", () => {
      wrapper.remove();
      this.refreshTaskPhaseOptions();
    });
    // Refresh options when user types phase name (for better labels), and when added
    const phaseNameInput = wrapper.querySelector(".phase-name");
    phaseNameInput?.addEventListener("input", () =>
      this.refreshTaskPhaseOptions()
    );
    // Initial refresh to reflect new phase
    setTimeout(() => this.refreshTaskPhaseOptions(), 0);
    return wrapper;
  }

  buildTaskRow(task = {}) {
    const id = task.taskId || "task_" + crypto.randomUUID();
    const wrapper = document.createElement("div");
    wrapper.className = "card mb-2";
    const contractorOptions = [
      `<option value="">Select contractor...</option>`,
      ...this.contractors.map(
        (c) => `<option value="${c.userId}">${c.fullName} (${c.email})</option>`
      ),
    ].join("");
    const phaseOptions = this.buildPhaseOptionsHtml(task.phaseId);
    wrapper.innerHTML = `
      <div class="card-body">
        <div class="row g-2 align-items-end">
          <div class="col-md-3">
            <label class="form-label">Task Name</label>
            <input type="text" class="form-control task-name" data-id="${id}" value="${
      task.name || ""
    }" />
          </div>
          <div class="col-md-2">
            <label class="form-label">Phase</label>
            <select class="form-select task-phase-select">${phaseOptions}</select>
          </div>
          <div class="col-md-2">
            <label class="form-label">Contractor</label>
            <select class="form-select task-contractor">${contractorOptions}</select>
          </div>
          <div class="col-md-2">
            <label class="form-label">Budget</label>
            <div class="input-group">
              <span class="input-group-text">R</span>
              <input type="number" class="form-control task-budget" value="${
                task.budget || ""
              }" step="0.01" min="0" />
            </div>
          </div>
          <div class="col-md-2">
            <label class="form-label">Due</label>
            <input type="date" class="form-control task-due" value="${
              task.dueDate ? new Date(task.dueDate).toISOString().split('T')[0] : ""
            }" />
          </div>
          <div class="col-md-1 text-end">
            <button type="button" class="btn btn-outline-danger btn-sm remove-task">Remove</button>
          </div>
        </div>
        <div class="row g-2 mt-2">
          <div class="col-md-6">
            <label class="form-label">Start Date</label>
            <input type="date" class="form-control task-start" value="${
              task.startDate ? new Date(task.startDate).toISOString().split('T')[0] : ""
            }" />
          </div>
          <div class="col-md-6">
            <label class="form-label">Description</label>
            <input type="text" class="form-control task-desc" value="${
              task.description || ""
            }" placeholder="Task description..." />
          </div>
        </div>
      </div>`;
    wrapper.querySelector(".remove-task").addEventListener("click", () => {
      wrapper.remove();
    });
    return wrapper;
  }

  buildPhaseOptionsHtml(selectedPhaseId) {
    const phases = this.getCurrentPhases();
    const opts = [
      '<option value="">Select phase...</option>',
      ...phases.map(
        (p) =>
          `<option value="${p.phaseId}" ${
            p.phaseId === (selectedPhaseId || "") ? "selected" : ""
          }>${p.name || p.phaseId}</option>`
      ),
    ];
    return opts.join("");
  }

  getCurrentPhases() {
    const container = document.getElementById("phasesContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card")).map((card) => {
      const id = card.querySelector(".phase-name")?.dataset.id || "";
      const name = card.querySelector(".phase-name")?.value || "";
      return { phaseId: id, name };
    });
  }

  refreshTaskPhaseOptions() {
    const tasksContainer = document.getElementById("tasksContainer");
    if (!tasksContainer) return;
    const selects = tasksContainer.querySelectorAll(".task-phase-select");
    const currentPhases = this.getCurrentPhases();
    selects.forEach((sel) => {
      const current = sel.value;
      sel.innerHTML = this.buildPhaseOptionsHtml(current);
    });
  }

  bindEvents() {
    // existing
    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("show.bs.modal", () => {
        this.resetWizard();
      });
    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("hidden.bs.modal", () => {
        this.resetWizard();
      });
    document.getElementById("nextStepBtn")?.addEventListener("click", () => {
      this.nextStep();
    });
    document.getElementById("prevStepBtn")?.addEventListener("click", () => {
      this.previousStep();
    });
    document
      .getElementById("createProjectBtn")
      ?.addEventListener("click", () => {
        this.finalizeProject();
      });

    // inputs change validation
    document
      .querySelectorAll(
        "#projectCreationForm input, #projectCreationForm select, #projectCreationForm textarea"
      )
      .forEach((element) => {
        element.addEventListener("change", () => {
          this.validateCurrentStep();
        });
      });

    // dates
    document.getElementById("startDate")?.addEventListener("change", () => {
      this.validateDateRange();
    });
    document
      .getElementById("endDatePlanned")
      ?.addEventListener("change", () => {
        this.validateDateRange();
      });

    // dynamic rows
    document.getElementById("addPhaseBtn")?.addEventListener("click", () => {
      document
        .getElementById("phasesContainer")
        ?.appendChild(this.buildPhaseRow());
    });
    document.getElementById("addTaskBtn")?.addEventListener("click", () => {
      document
        .getElementById("tasksContainer")
        ?.appendChild(this.buildTaskRow());
    });
  }

  collectPhases() {
    const container = document.getElementById("phasesContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card")).map((card) => {
      return {
        phaseId: card.querySelector(".phase-name")?.dataset.id || "",
        projectId: this.projectId,
        name: card.querySelector(".phase-name")?.value || "",
        description: card.querySelector(".phase-desc")?.value || "",
        startDate:
          this.parseDateInput(card.querySelector(".phase-start")?.value) ||
          new Date().toISOString(),
        endDate:
          this.parseDateInput(card.querySelector(".phase-end")?.value) ||
          new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
        status: "Pending",
        progress: 0,
        budget: parseFloat(card.querySelector(".phase-budget")?.value) || 0,
        assignedTo: "",
      };
    });
  }

  collectTasks() {
    const container = document.getElementById("tasksContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card")).map((card) => {
      return {
        taskId: card.querySelector(".task-name")?.dataset.id || "",
        projectId: this.projectId,
        name: card.querySelector(".task-name")?.value || "",
        phaseId: card.querySelector(".task-phase-select")?.value || "",
        assignedTo: card.querySelector(".task-contractor")?.value || "",
        startDate:
          this.parseDateInput(card.querySelector(".task-start")?.value) ||
          new Date().toISOString(),
        dueDate:
          this.parseDateInput(card.querySelector(".task-due")?.value) ||
          new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        status: "Pending",
        priority: "Medium",
        progress: 0,
        estimatedHours: 0,
        actualHours: 0,
        budget: parseFloat(card.querySelector(".task-budget")?.value) || 0,
        description: card.querySelector(".task-desc")?.value || "",
      };
    });
  }

  collectProjectData() {
    const form = document.getElementById("projectCreationForm");
    if (!form) {
      console.error("Project creation form not found");
      return {
        projectId: this.projectId || this.generateProjectId(),
        name: "",
        description: "",
        clientId: "",
        startDate: null,
        endDatePlanned: null,
        budgetPlanned: 0,
        status: "Planning",
      };
    }

    // Ensure we have a valid projectId
    if (!this.projectId) {
      this.projectId = this.generateProjectId();
      console.log("Generated new projectId:", this.projectId);
    }

    return {
      projectId: this.projectId,
      name: form.querySelector("#projectName")?.value || "",
      description: form.querySelector("#projectDescription")?.value || "",
      clientId: form.querySelector("#clientSelect")?.value || "",
      startDate: this.parseDateInput(form.querySelector("#startDate")?.value),
      endDatePlanned: this.parseDateInput(
        form.querySelector("#endDatePlanned")?.value
      ),
      budgetPlanned:
        parseFloat(form.querySelector("#budgetPlanned")?.value) || 0,
      status: "Planning", // Set to Planning instead of Draft
      // projectManagerId will be set by the server from the current user
    };
  }

  async finalizeProject() {
    console.log("finalizeProject called - using NEW method");
    console.log("Current projectId:", this.projectId);

    await this.ensureDraft();

    // Collect all project data
    const projectData = this.collectProjectData();
    const phases = this.collectPhases().filter((p) => p.name?.trim());
    const tasks = this.collectTasks().filter((t) => t.name?.trim());

    console.log("Project Data:", projectData);
    console.log("Phases count:", phases.length);
    console.log("Tasks count:", tasks.length);

    // Add validation to ensure projectData is not empty
    if (!projectData || Object.keys(projectData).length === 0) {
      console.error("Project data is empty or null");
      this.showError("Project data is missing. Please check the form.");
      return;
    }

    // Validate required fields
    if (!projectData.name || projectData.name.trim() === "") {
      this.showError("Project name is required.");
      return;
    }

    if (!projectData.clientId || projectData.clientId.trim() === "") {
      this.showError("Please select a client for this project.");
      return;
    }

    try {
      // Use the new complete project creation endpoint
      const completeRequest = {
        project: projectData,
        phases: phases,
        tasks: tasks,
      };

      console.log("Calling SaveProject endpoint /ProjectManager/SaveProject");
      const createRes = await fetch(`/ProjectManager/SaveProject`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(completeRequest),
      });

      console.log("Response status:", createRes.status);

      if (!createRes.ok) {
        console.error("HTTP Error:", createRes.status, createRes.statusText);
        this.showError(
          `Server error: ${createRes.status} ${createRes.statusText}`
        );
        return;
      }

      const createData = await createRes.json();
      console.log("Response data:", createData);
      console.log("Success:", createData?.success);
      console.log("Message:", createData?.message);
      console.log("Status:", createData?.status);

      if (createData?.success) {
        this.showSuccess(
          createData.message || "Project created and moved to Planning status."
        );
        this.closeWizard();
        setTimeout(() => window.location.reload(), 1200);
      } else {
        console.error("API Error:", createData);
        const errorMessage = createData?.error || "Failed to create project.";

        // Check if it's an authentication error
        if (
          errorMessage.includes("Authentication error") ||
          errorMessage.includes("project manager ID")
        ) {
          this.showError(
            "Authentication error: Please log out and log in again to create projects."
          );
        } else {
          this.showError(errorMessage);
        }
      }
    } catch (e) {
      console.error("Project creation failed", e);
      this.showError("Project creation failed");
    }
  }

  generateProjectId() {
    return (
      "proj_" +
      Math.random().toString(36).substr(2, 9) +
      "_" +
      Date.now().toString(36)
    );
  }

  resetWizard() {
    this.currentStep = 1;
    this.formData = {};

    // Reset form
    const form = document.getElementById("projectCreationForm");
    if (form) {
      form.reset();
    }

    // Clear validation states
    document.querySelectorAll(".is-invalid").forEach((element) => {
      element.classList.remove("is-invalid");
    });

    // Reset wizard display
    this.updateWizardDisplay();
  }

  closeWizard() {
    const modal = bootstrap.Modal.getInstance(
      document.getElementById("projectCreationWizardModal")
    );
    if (modal) {
      modal.hide();
    }
  }

  showLoading(show) {
    const overlay = document.getElementById("wizardLoadingOverlay");
    if (overlay) {
      overlay.style.display = show ? "flex" : "none";
    }
  }

  showSuccess(message) {
    this.toast(message, "success");
  }

  showError(message) {
    this.toast(message, "danger");
  }

  toast(message, type = "info") {
    try {
      const containerId = "toast-container";
      let container = document.getElementById(containerId);
      if (!container) {
        container = document.createElement("div");
        container.id = containerId;
        container.className = "toast-container position-fixed top-0 end-0 p-3";
        document.body.appendChild(container);
      }

      const toastEl = document.createElement("div");
      toastEl.className = `toast align-items-center text-bg-${type} border-0`;
      toastEl.setAttribute("role", "alert");
      toastEl.setAttribute("aria-live", "assertive");
      toastEl.setAttribute("aria-atomic", "true");
      toastEl.innerHTML = `
        <div class="d-flex">
          <div class="toast-body">${message}</div>
          <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>`;
      container.appendChild(toastEl);
      const bsToast = new bootstrap.Toast(toastEl, { delay: 2500 });
      bsToast.show();
      toastEl.addEventListener("hidden.bs.toast", () => toastEl.remove());
    } catch (e) {
      // fallback
      console.log(message);
    }
  }
}

// Global function to open the wizard (called from dashboard)
function openCreateProjectWizard() {
  const modal = new bootstrap.Modal(
    document.getElementById("projectCreationWizardModal")
  );
  modal.show();
}

// Initialize the wizard when the page loads
document.addEventListener("DOMContentLoaded", function () {
  // Only initialize if the wizard modal exists on the page
  if (document.getElementById("projectCreationWizardModal")) {
    window.__pcwInstance = new ProjectCreationWizard();
  }
});
