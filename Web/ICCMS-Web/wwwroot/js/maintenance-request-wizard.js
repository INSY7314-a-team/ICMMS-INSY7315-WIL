/*
 * Maintenance Request Wizard JavaScript
 * Handles the 2-step wizard for approving maintenance requests with phases and tasks
 */

class MaintenanceRequestWizard {
  constructor() {
    this.currentStep = 1;
    this.totalSteps = 2; // Phases, Tasks
    this.contractors = [];
    this.requestId = null;
    this.projectId = null;

    this.init();
  }

  init() {
    this.bindEvents();
  }

  bindEvents() {
    // Modal events
    document
      .getElementById("maintenanceRequestWizardModal")
      ?.addEventListener("show.bs.modal", (event) => {
        const button = event.relatedTarget;
        if (button) {
          this.requestId = button.getAttribute("data-request-id");
          this.projectId = button.getAttribute("data-project-id");
          
          document.getElementById("mrwRequestId").value = this.requestId || "";
          document.getElementById("mrwProjectId").value = this.projectId || "";
          
          this.resetWizard();
        }
      });

    document
      .getElementById("maintenanceRequestWizardModal")
      ?.addEventListener("shown.bs.modal", () => {
        this.initializeWizard();
      });

    document
      .getElementById("maintenanceRequestWizardModal")
      ?.addEventListener("hidden.bs.modal", () => {
        this.resetWizard();
      });

    // Navigation buttons
    document.getElementById("mrwNextStepBtn")?.addEventListener("click", () => {
      this.nextStep();
    });

    document.getElementById("mrwPrevStepBtn")?.addEventListener("click", () => {
      this.previousStep();
    });

    document.getElementById("mrwApproveBtn")?.addEventListener("click", () => {
      this.approveRequest();
    });

    // Add phase/task buttons
    document.getElementById("mrwAddPhaseBtn")?.addEventListener("click", () => {
      document
        .getElementById("mrwPhasesContainer")
        ?.appendChild(this.buildPhaseRow());
    });

    document.getElementById("mrwAddTaskBtn")?.addEventListener("click", () => {
      document
        .getElementById("mrwTasksContainer")
        ?.appendChild(this.buildTaskRow());
    });
  }

  async initializeWizard() {
    console.log("Maintenance Request Wizard: Initializing...");
    await this.loadContractors();
    this.updateWizardDisplay();
  }

  async loadContractors() {
    try {
      const response = await fetch("/ProjectManager/GetContractors");
      if (response.ok) {
        this.contractors = await response.json();
        console.log("Loaded contractors:", this.contractors.length);
      }
    } catch (e) {
      console.warn("Failed to load contractors", e);
      this.showError("Failed to load contractors. Please try again.");
    }
  }

  resetWizard() {
    this.currentStep = 1;
    document.getElementById("mrwPhasesContainer").innerHTML = "";
    document.getElementById("mrwTasksContainer").innerHTML = "";
    this.updateWizardDisplay();
  }

  nextStep() {
    if (this.currentStep < this.totalSteps) {
      // Validate current step before proceeding
      if (this.validateCurrentStep()) {
        this.currentStep++;
        this.updateWizardDisplay();
        if (this.currentStep === 2) {
          // Refresh phase options for tasks when moving to step 2
          this.refreshTaskPhaseOptions();
        }
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
    if (this.currentStep === 1) {
      // Validate phases - at least one phase must have a name
      const phases = this.collectPhases();
      if (phases.length === 0) {
        this.showError("Please add at least one phase.");
        return false;
      }
      const hasValidPhase = phases.some(p => p.name && p.name.trim() !== "");
      if (!hasValidPhase) {
        this.showError("Please provide a name for at least one phase.");
        return false;
      }
    } else if (this.currentStep === 2) {
      // Validate tasks - at least one task must have a name
      const tasks = this.collectTasks();
      if (tasks.length === 0) {
        this.showError("Please add at least one task.");
        return false;
      }
      const hasValidTask = tasks.some(t => t.name && t.name.trim() !== "");
      if (!hasValidTask) {
        this.showError("Please provide a name for at least one task.");
        return false;
      }
      // Check that all tasks have a phase assigned
      const tasksWithoutPhase = tasks.filter(t => !t.phaseId || t.phaseId === "");
      if (tasksWithoutPhase.length > 0) {
        this.showError("Please assign all tasks to a phase.");
        return false;
      }
    }
    return true;
  }

  updateWizardDisplay() {
    // Update progress bar
    const progress = (this.currentStep / this.totalSteps) * 100;
    const progressBar = document.getElementById("mrwWizardProgress");
    if (progressBar) {
      progressBar.style.width = `${progress}%`;
    }

    // Update step counter
    const currentStepEl = document.getElementById("mrwCurrentStep");
    if (currentStepEl) {
      currentStepEl.textContent = this.currentStep;
    }

    // Update step titles
    const stepTitles = ["Phases", "Tasks & Contractors"];
    const stepTitleEl = document.getElementById("mrwStepTitle");
    if (stepTitleEl) {
      stepTitleEl.textContent = stepTitles[this.currentStep - 1];
    }

    // Show/hide steps
    for (let i = 1; i <= this.totalSteps; i++) {
      const stepElement = document.getElementById(`mrwStep${i}`);
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
  }

  updateNavigationButtons() {
    const prevBtn = document.getElementById("mrwPrevStepBtn");
    const nextBtn = document.getElementById("mrwNextStepBtn");
    const approveBtn = document.getElementById("mrwApproveBtn");

    if (prevBtn) {
      prevBtn.style.display = this.currentStep > 1 ? "inline-block" : "none";
    }

    if (nextBtn && approveBtn) {
      if (this.currentStep === this.totalSteps) {
        nextBtn.style.display = "none";
        approveBtn.style.display = "inline-block";
      } else {
        nextBtn.style.display = "inline-block";
        approveBtn.style.display = "none";
      }
    }
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
    // Refresh options when user types phase name
    const phaseNameInput = wrapper.querySelector(".phase-name");
    phaseNameInput?.addEventListener("input", () =>
      this.refreshTaskPhaseOptions()
    );
    // Initial refresh
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
            <label class="form-label">Priority</label>
            <select class="form-select task-priority">
              <option value="Low" ${task.priority === "Low" ? "selected" : ""}>Low</option>
              <option value="Medium" ${task.priority === "Medium" || !task.priority ? "selected" : ""}>Medium</option>
              <option value="High" ${task.priority === "High" ? "selected" : ""}>High</option>
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label">Due Date</label>
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
    const container = document.getElementById("mrwPhasesContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card")).map((card) => {
      const id = card.querySelector(".phase-name")?.dataset.id || "";
      const name = card.querySelector(".phase-name")?.value || "";
      return { phaseId: id, name };
    });
  }

  refreshTaskPhaseOptions() {
    const tasksContainer = document.getElementById("mrwTasksContainer");
    if (!tasksContainer) return;
    const selects = tasksContainer.querySelectorAll(".task-phase-select");
    const currentPhases = this.getCurrentPhases();
    selects.forEach((sel) => {
      const current = sel.value;
      sel.innerHTML = this.buildPhaseOptionsHtml(current);
    });
  }

  collectPhases() {
    const container = document.getElementById("mrwPhasesContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card"))
      .map((card) => {
        const name = card.querySelector(".phase-name")?.value?.trim() || "";
        // Only include phases with names
        if (!name) return null;
        return {
          phaseId: card.querySelector(".phase-name")?.dataset.id || "",
          projectId: this.projectId,
          name: name,
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
        };
      })
      .filter(p => p !== null);
  }

  collectTasks() {
    const container = document.getElementById("mrwTasksContainer");
    if (!container) return [];
    return Array.from(container.querySelectorAll(".card"))
      .map((card) => {
        const name = card.querySelector(".task-name")?.value?.trim() || "";
        // Only include tasks with names
        if (!name) return null;
        return {
          taskId: card.querySelector(".task-name")?.dataset.id || "",
          projectId: this.projectId,
          name: name,
          phaseId: card.querySelector(".task-phase-select")?.value || "",
          assignedTo: card.querySelector(".task-contractor")?.value || "",
          startDate:
            this.parseDateInput(card.querySelector(".task-start")?.value) ||
            new Date().toISOString(),
          dueDate:
            this.parseDateInput(card.querySelector(".task-due")?.value) ||
            new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
          status: "Pending",
          priority: card.querySelector(".task-priority")?.value || "Medium",
          progress: 0,
          estimatedHours: 0,
          actualHours: 0,
          description: card.querySelector(".task-desc")?.value || "",
        };
      })
      .filter(t => t !== null);
  }

  parseDateInput(dateString) {
    if (!dateString) return null;
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return null;
    return date.toISOString();
  }

  showError(message) {
    // Simple alert for now - can be enhanced with toast notifications
    alert(message);
  }

  showLoading(show) {
    const overlay = document.getElementById("mrwWizardLoadingOverlay");
    if (overlay) {
      overlay.style.display = show ? "flex" : "none";
    }
  }

  async approveRequest() {
    if (!this.validateCurrentStep()) {
      return;
    }

    const phases = this.collectPhases();
    const tasks = this.collectTasks();

    if (phases.length === 0) {
      this.showError("Please add at least one phase.");
      return;
    }

    if (tasks.length === 0) {
      this.showError("Please add at least one task.");
      return;
    }

    this.showLoading(true);

    try {
      const response = await fetch(
        `/ProjectManager/ApproveMaintenanceRequest?requestId=${encodeURIComponent(this.requestId)}`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken:
              (document.querySelector('input[name="__RequestVerificationToken"]') ||
                {}).value || "",
          },
          body: JSON.stringify({
            requestId: this.requestId,
            projectId: this.projectId,
            phases: phases,
            tasks: tasks,
          }),
        }
      );

      const result = await response.json();

      if (response.ok && result.success) {
        alert("Maintenance request approved successfully!");
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("maintenanceRequestWizardModal")
        );
        if (modal) {
          modal.hide();
        }
        // Redirect to project detail page instead of reload
        if (this.projectId) {
          window.location.href = `/ProjectManager/ProjectDetail?projectId=${this.projectId}`;
        } else {
          location.reload();
        }
      } else {
        this.showError(
          result.error || "Failed to approve maintenance request."
        );
      }
    } catch (error) {
      console.error("Error approving maintenance request:", error);
      this.showError("An error occurred. Please try again.");
    } finally {
      this.showLoading(false);
    }
  }
}

// Initialize wizard when DOM is ready
let maintenanceRequestWizardInstance = null;
document.addEventListener("DOMContentLoaded", function () {
  maintenanceRequestWizardInstance = new MaintenanceRequestWizard();
});

