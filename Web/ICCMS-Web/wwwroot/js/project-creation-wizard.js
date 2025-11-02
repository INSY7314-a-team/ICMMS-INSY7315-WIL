/*
 * Project Creation Wizard JavaScript
 * Handles the multi-step project creation process
 */

console.log("CREATE WIZARD: Script file loaded!");

class ProjectCreationWizard {
  constructor() {
    this.currentStep = 1;
    this.totalSteps = 5; // Basic, Budget, Phases, Tasks, Review
    this.formData = {};
    this.clients = [];
    this.contractors = [];
    this.projectId = null; // Will be set when loading existing draft or creating new draft
    this.autosaveIntervalId = null;
    this.isEditingDraft = false; // Flag to track if we're editing an existing draft

    this.init();
  }

  init() {
    this.bindEvents();
    this.setupFormValidation();
    this.setupAutosave();
    // Don't load clients/contractors on init - load them when modal opens
  }

  bindEvents() {
    // Modal events
    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("show.bs.modal", () => {
        // Only reset if we're not editing a draft
        const modal = document.getElementById("projectCreationWizardModal");
        const draftProjectId = modal?.getAttribute("data-draft-project-id");
        if (!draftProjectId) {
          this.resetWizard();
        }
      });

    document
      .getElementById("projectCreationWizardModal")
      ?.addEventListener("shown.bs.modal", () => {
        this.initializeWizard();
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

    // Save as Draft button
    document
      .getElementById("saveAsDraftBtn")
      ?.addEventListener("click", () => {
        this.saveAsDraft();
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
      console.log("🔥 CREATE WIZARD: loadClients() called!");
      const response = await fetch("/ProjectManager/GetClients");
      console.log("🔥 CREATE WIZARD: API response status:", response.status);
      if (response.ok) {
        this.clients = await response.json();
        console.log("🔥 CREATE WIZARD: Loaded clients:", this.clients);
        this.populateClientSelect();
      } else {
        console.error("🔥 CREATE WIZARD: Failed to load clients:", response.statusText);
        this.showError("Failed to load clients. Please refresh the page.");
      }
    } catch (error) {
      console.error("🔥 CREATE WIZARD: Error loading clients:", error);
      this.showError("Error loading clients. Please check your connection.");
    }
  }

  populateClientSelect() {
    console.log("🔥 CREATE WIZARD: populateClientSelect() called!");
    const clientSelect = document.getElementById("clientSelect");
    console.log("🔥 CREATE WIZARD: clientSelect element:", clientSelect);
    console.log("🔥 CREATE WIZARD: clients array:", this.clients);
    if (clientSelect && this.clients) {
      console.log("🔥 CREATE WIZARD: Populating dropdown with", this.clients.length, "clients");
      // Clear existing options except the first one
      clientSelect.innerHTML = '<option value="">Select a client...</option>';
      
      this.clients.forEach(client => {
        const option = document.createElement("option");
        option.value = client.userId;
        option.textContent = `${client.fullName} (${client.email})`;
        clientSelect.appendChild(option);
      });
      console.log("🔥 CREATE WIZARD: Dropdown populated with", clientSelect.options.length, "options");
    } else {
      console.log("🔥 CREATE WIZARD: clientSelect element not found or clients not loaded");
    }
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
        
        // Auto-save as draft when reaching phases page (step 3)
        if (this.currentStep === 3 && !this.projectId) {
          this.autoSaveAsDraft();
        }
        
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

  async loadDraftProject(projectId) {
    console.log("loadDraftProject called with ID:", projectId);
    try {
      const response = await fetch(`/ProjectManager/GetProject/${projectId}`);
      console.log("Response status:", response.status);
      
      if (response.ok) {
        const projectData = await response.json();
        console.log("Received project data:", projectData);
        
        if (projectData && projectData.status === "Draft") {
          this.projectId = projectId;
          this.isEditingDraft = true;
          
          // Populate form fields with existing data
          this.populateFormWithDraftData(projectData);
          
          // Load phases and tasks separately
          await this.loadProjectPhases(projectId);
          await this.loadProjectTasks(projectId);
          
          console.log("Successfully loaded draft project:", projectId);
          return true;
        } else {
          console.warn("Project is not a draft or data is invalid:", projectData);
        }
      } else {
        console.error("Failed to fetch project:", response.status, response.statusText);
      }
    } catch (error) {
      console.error("Failed to load draft project:", error);
    }
    return false;
  }

  async loadProjectPhases(projectId) {
    try {
      console.log("Loading phases for project:", projectId);
      const response = await fetch(`/ProjectManager/GetProjectPhases?id=${projectId}`);
      if (response.ok) {
        const phases = await response.json();
        console.log("Received phases:", phases);
        
        if (phases && phases.length > 0) {
          const phasesContainer = document.getElementById("phasesContainer");
          if (phasesContainer) {
            phasesContainer.innerHTML = ""; // Clear existing phases
            phases.forEach(phase => {
              const phaseElement = this.buildPhaseRow(phase);
              phasesContainer.appendChild(phaseElement);
            });
            console.log("Loaded", phases.length, "phases");
          }
        }
      } else {
        console.warn("Failed to load phases:", response.status);
      }
    } catch (error) {
      console.warn("Error loading phases:", error);
    }
  }

  async loadProjectTasks(projectId) {
    try {
      console.log("Loading tasks for project:", projectId);
      const response = await fetch(`/ProjectManager/GetProjectTasks?id=${projectId}`);
      if (response.ok) {
        const tasks = await response.json();
        console.log("Received tasks:", tasks);
        
        if (tasks && tasks.length > 0) {
          const tasksContainer = document.getElementById("tasksContainer");
          if (tasksContainer) {
            tasksContainer.innerHTML = ""; // Clear existing tasks
            tasks.forEach(task => {
              const taskElement = this.buildTaskRow(task);
              tasksContainer.appendChild(taskElement);
            });
            console.log("Loaded", tasks.length, "tasks");
          }
        }
      } else {
        console.warn("Failed to load tasks:", response.status);
      }
    } catch (error) {
      console.warn("Error loading tasks:", error);
    }
  }

  populateFormWithDraftData(projectData) {
    console.log("Populating form with draft data:", projectData);
    
    // Populate basic information
    if (projectData.name) {
      const nameField = document.getElementById("projectName");
      if (nameField) {
        nameField.value = projectData.name;
        console.log("Set project name:", projectData.name);
      }
    }
    
    if (projectData.description) {
      const descField = document.getElementById("projectDescription");
      if (descField) {
        descField.value = projectData.description;
        console.log("Set project description:", projectData.description);
      }
    }
    
    if (projectData.clientId) {
      const clientField = document.getElementById("clientSelect");
      if (clientField) {
        clientField.value = projectData.clientId;
        console.log("Set client ID:", projectData.clientId);
      }
    }
    
    if (projectData.budgetPlanned) {
      const budgetField = document.getElementById("budgetPlanned");
      if (budgetField) {
        budgetField.value = projectData.budgetPlanned;
        console.log("Set budget:", projectData.budgetPlanned);
      }
    }
    
    if (projectData.startDate) {
      const startField = document.getElementById("startDate");
      if (startField) {
        startField.value = new Date(projectData.startDate).toISOString().split('T')[0];
        console.log("Set start date:", projectData.startDate);
      }
    }
    
    if (projectData.endDatePlanned) {
      const endField = document.getElementById("endDatePlanned");
      if (endField) {
        endField.value = new Date(projectData.endDatePlanned).toISOString().split('T')[0];
        console.log("Set end date:", projectData.endDatePlanned);
      }
    }
  }

  async initializeWizard() {
    console.log("🔥 CREATE WIZARD: initializeWizard() called!");
    // Load clients and contractors first (like edit wizard)
    await this.loadClients();
    await this.loadContractors();
    
    // Check if we're editing an existing draft project
    const modal = document.getElementById("projectCreationWizardModal");
    const draftProjectId = modal?.getAttribute("data-draft-project-id");
    
    if (draftProjectId) {
      console.log("Loading draft project:", draftProjectId);
      
      // Add a small delay to ensure form elements are rendered
      setTimeout(async () => {
        const loaded = await this.loadDraftProject(draftProjectId);
        if (loaded) {
          console.log("Initialized wizard with existing draft project:", draftProjectId);
          // Update the modal title to indicate we're editing
          const titleElement = document.getElementById("projectCreationWizardModalLabel");
          if (titleElement) {
            titleElement.innerHTML = `<i class="fa-solid fa-edit me-2" style="color: #F7EC59;"></i>Edit Draft Project`;
          }
        } else {
          console.error("Failed to load draft project:", draftProjectId);
        }
      }, 100);
    } else {
      console.log("Initialized wizard for new project creation");
    }
  }

  async createDraftProject() {
    if (this.projectId) return this.projectId; // Already have a draft
    
    try {
      const projectData = this.collectProjectData();
      projectData.status = "Draft"; // Ensure status is Draft

      const res = await fetch("/ProjectManager/SaveProject", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          project: projectData,
          phases: this.collectPhases(),
          tasks: this.collectTasks()
        }),
      });

      const data = await res.json();
      if (data?.success) {
        this.projectId = data.projectId;
        console.log("Draft project created with all form data:", this.projectId);
        return this.projectId;
      }
    } catch (e) {
      console.warn("CreateDraftProject failed", e);
    }
    return null;
  }

  setupAutosave() {
    // Remove automatic draft creation - only create when user explicitly saves as draft
    // Autosave every 10s (only if we have a projectId)
    this.autosaveIntervalId = setInterval(() => {
      if (this.projectId) {
        this.autosave();
      }
    }, 10000);

    // Autosave on navigation/unload (only if we have a projectId)
    window.addEventListener("beforeunload", () => {
      if (this.projectId) {
      this.autosave(true);
      }
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
    // Duplicate event listener removed - handled above
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
        // status will be set by the calling function
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
      // status will be set by the calling function (Draft, Planning, etc.)
      // projectManagerId will be set by the server from the current user
    };
  }

  async autoSaveAsDraft() {
    console.log("Auto-saving as draft when reaching phases page");
    
    // Collect current project data
    const projectData = this.collectProjectData();
    
    // Validate required fields for auto-save
    if (!projectData.name || projectData.name.trim() === "") {
      console.log("Skipping auto-save: Project name required");
      return;
    }

    if (!projectData.clientId || projectData.clientId.trim() === "") {
      console.log("Skipping auto-save: Client required");
      return;
    }

    try {
      // Set project status to Draft
      projectData.status = "Draft";
      console.log("Setting project status to Draft:", projectData.status);

      // Use the complete project creation endpoint
      const completeRequest = {
        project: projectData,
        phases: [], // Empty phases for now
        tasks: [],  // Empty tasks for now
      };

      console.log("Auto-saving as draft:", completeRequest);
      console.log("Request payload status:", completeRequest.project.status);
      
      const createRes = await fetch(`/ProjectManager/SaveProject`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(completeRequest),
      });

      console.log("Response status:", createRes.status);
      
      if (createRes.ok) {
        const data = await createRes.json();
        console.log("Response data:", data);
        console.log("Created project status:", data?.project?.status || "Not provided in response");
        
        if (data?.success) {
          this.projectId = data.projectId;
          console.log("Auto-saved as draft successfully:", this.projectId);
          
          // Show subtle notification
          this.showSuccess("Project auto-saved as draft", 2000);
        } else {
          console.warn("Auto-save failed:", data?.message || "Unknown error");
        }
      } else {
        console.warn(`Auto-save failed: ${createRes.status} ${createRes.statusText}`);
      }
    } catch (error) {
      console.warn("Auto-save failed:", error);
    }
  }

  async saveAsDraft() {
    
    // Collect all project data
    const projectData = this.collectProjectData();
    const phases = this.collectPhases().filter((p) => p.name?.trim());
    const tasks = this.collectTasks().filter((t) => t.name?.trim());

    // Validate required fields for draft
    if (!projectData.name || projectData.name.trim() === "") {
      this.showError("Project name is required to save as draft.");
      return;
    }

    if (!projectData.clientId || projectData.clientId.trim() === "") {
      this.showError("Please select a client to save as draft.");
      return;
    }

    try {
      // Set project status to Draft
      projectData.status = "Draft";

      // Use the complete project creation endpoint
      const completeRequest = {
        project: projectData,
        phases: phases,
        tasks: tasks,
      };

      // If we have a projectId (from auto-save), include it
      if (this.projectId) {
        completeRequest.project.projectId = this.projectId;
      }

      console.log("Saving as draft:", completeRequest);
      const createRes = await fetch(`/ProjectManager/SaveProject`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(completeRequest),
      });

      if (createRes.ok) {
        const data = await createRes.json();
        if (data?.success) {
          this.projectId = data.projectId;
          this.showSuccess("Project saved as draft successfully!");
          
          // Close the modal after successful save
          setTimeout(() => {
            const modal = bootstrap.Modal.getInstance(document.getElementById("projectCreationWizardModal"));
            if (modal) {
              modal.hide();
            }
            // Reload the page to show the new draft
            window.location.reload();
          }, 1500);
        } else {
          this.showError("Failed to save draft: " + (data?.message || "Unknown error"));
        }
      } else {
        this.showError(`Failed to save draft: ${createRes.status} ${createRes.statusText}`);
      }
    } catch (error) {
      console.error("Save as draft failed:", error);
      this.showError("Failed to save draft: " + error.message);
    }
  }

  async finalizeProject() {
    console.log("Current projectId:", this.projectId);

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
      // Set project status to Planning (finalized project)
      projectData.status = "Planning";

      // Use the complete project creation endpoint
      const completeRequest = {
        project: projectData,
        phases: phases,
        tasks: tasks,
      };

      // If we have a projectId (from auto-save), include it
      if (this.projectId) {
        completeRequest.project.projectId = this.projectId;
      }

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
    this.projectId = null;
    this.isEditingDraft = false;

    // Reset form
    const form = document.getElementById("projectCreationForm");
    if (form) {
      form.reset();
    }

    // Clear phases and tasks containers
    const phasesContainer = document.getElementById("phasesContainer");
    if (phasesContainer) {
      phasesContainer.innerHTML = "";
    }

    const tasksContainer = document.getElementById("tasksContainer");
    if (tasksContainer) {
      tasksContainer.innerHTML = "";
    }

    // Reset modal title
    const titleElement = document.getElementById("projectCreationWizardModalLabel");
    if (titleElement) {
      titleElement.innerHTML = `<i class="fa-solid fa-plus-circle me-2" style="color: #F7EC59;"></i>Create Project`;
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

  showSuccess(message, duration = 3000) {
    this.toast(message, "success", duration);
  }

  showError(message) {
    this.toast(message, "danger");
  }

  toast(message, type = "info", duration = 3000) {
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
      const bsToast = new bootstrap.Toast(toastEl, { delay: duration });
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
  console.log("🔥 CREATE WIZARD: Script loaded and DOMContentLoaded fired!");
  // Only initialize if the wizard modal exists on the page
  if (document.getElementById("projectCreationWizardModal")) {
    console.log("🔥 CREATE WIZARD: Modal found, creating instance!");
    window.__pcwInstance = new ProjectCreationWizard();
    console.log("🔥 CREATE WIZARD: Instance created:", window.__pcwInstance);
  } else {
    console.log("🔥 CREATE WIZARD: Modal NOT found!");
  }
});
