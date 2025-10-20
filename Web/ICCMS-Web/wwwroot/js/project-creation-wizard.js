/**
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
  }

  async ensureDraft() {
    if (this.projectId) return;
    try {
      const res = await fetch("/ProjectManager/StartDraft", { method: "POST" });
      const data = await res.json();
      if (data?.success) {
        this.projectId = data.projectId;
      }
    } catch (e) {
      console.warn("StartDraft failed", e);
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

    // Gather basic info only for autosave (steps 1-2)
    const payload = {
      ProjectId: this.projectId,
      Name: document.getElementById("projectName")?.value || "",
      ClientId: document.getElementById("clientSelect")?.value || "",
      Description: document.getElementById("projectDescription")?.value || "",
      BudgetPlanned: parseFloat(
        document.getElementById("budgetPlanned")?.value || "0"
      ),
      StartDate: this.parseDateInput(
        document.getElementById("startDate")?.value
      ),
      EndDatePlanned: this.parseDateInput(
        document.getElementById("endDatePlanned")?.value
      ),
      Status: "Draft",
    };

    try {
      await fetch(
        `/ProjectManager/AutosaveProject?id=${encodeURIComponent(
          this.projectId
        )}`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
          keepalive: isSync, // allow send on unload
        }
      );
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
          <div class="col-md-5">
            <label class="form-label">Phase Name</label>
            <input type="text" class="form-control phase-name" data-id="${id}" value="${
      phase.name || ""
    }" />
          </div>
          <div class="col-md-5">
            <label class="form-label">Description</label>
            <input type="text" class="form-control phase-desc" value="${
              phase.description || ""
            }" />
          </div>
          <div class="col-md-2 text-end">
            <button type="button" class="btn btn-outline-danger btn-sm remove-phase">Remove</button>
          </div>
        </div>
      </div>`;
    wrapper.querySelector(".remove-phase").addEventListener("click", () => {
      wrapper.remove();
    });
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
    wrapper.innerHTML = `
      <div class="card-body">
        <div class="row g-2 align-items-end">
          <div class="col-md-3">
            <label class="form-label">Task Name</label>
            <input type="text" class="form-control task-name" data-id="${id}" value="${
      task.name || ""
    }" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Phase</label>
            <input type="text" class="form-control task-phase" placeholder="PhaseId" value="${
              task.phaseId || ""
            }" />
          </div>
          <div class="col-md-3">
            <label class="form-label">Contractor</label>
            <select class="form-select task-contractor">${contractorOptions}</select>
          </div>
          <div class="col-md-2">
            <label class="form-label">Due</label>
            <input type="date" class="form-control task-due" value="" />
          </div>
          <div class="col-md-1 text-end">
            <button type="button" class="btn btn-outline-danger btn-sm remove-task">Remove</button>
          </div>
        </div>
      </div>`;
    wrapper.querySelector(".remove-task").addEventListener("click", () => {
      wrapper.remove();
    });
    return wrapper;
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
        phaseId: card.querySelector(".task-phase")?.value || "",
        assignedTo: card.querySelector(".task-contractor")?.value || "",
        dueDate: this.parseDateInput(card.querySelector(".task-due")?.value),
      };
    });
  }

  async finalizeProject() {
    await this.ensureDraft();

    // Persist phases and tasks first
    const phases = this.collectPhases().filter((p) => p.name?.trim());
    const tasks = this.collectTasks().filter((t) => t.name?.trim());

    try {
      if (phases.length) {
        await fetch(
          `/ProjectManager/SavePhases?id=${encodeURIComponent(this.projectId)}`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(phases),
          }
        );
      }
      if (tasks.length) {
        await fetch(
          `/ProjectManager/SaveTasks?id=${encodeURIComponent(this.projectId)}`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(tasks),
          }
        );
      }

      // Finalize -> Planning
      const res = await fetch(
        `/ProjectManager/FinalizeProject?id=${encodeURIComponent(
          this.projectId
        )}`,
        {
          method: "POST",
        }
      );
      const data = await res.json();
      if (data?.success) {
        this.showSuccess("Project moved to Planning.");
        this.closeWizard();
        setTimeout(() => window.location.reload(), 1200);
      } else {
        this.showError(data?.error || "Finalize failed.");
      }
    } catch (e) {
      console.error("Finalize failed", e);
      this.showError("Finalize failed");
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
    // You can implement a toast notification here
    console.log("Success:", message);
    // For now, just show an alert
    alert(message);
  }

  showError(message) {
    // You can implement a toast notification here
    console.error("Error:", message);
    // For now, just show an alert
    alert("Error: " + message);
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
