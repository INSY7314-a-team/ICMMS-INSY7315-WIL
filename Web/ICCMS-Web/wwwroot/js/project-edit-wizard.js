/*
 * Project Edit Wizard JavaScript
 * Handles editing existing draft projects
 */

class ProjectEditWizard {
  constructor() {
    this.currentStep = 1;
    this.totalSteps = 5; // Basic, Budget, Phases, Tasks, Review
    this.formData = {};
    this.clients = [];
    this.contractors = [];
    this.projectId = null;
    this.isEditingDraft = false;

    this.init();
  }

  init() {
    this.bindEvents();
    this.setupFormValidation();
    // Don't load clients/contractors on init - load them when modal opens
  }

  bindEvents() {
    // Modal events
    document
      .getElementById("projectEditWizardModal")
      ?.addEventListener("show.bs.modal", () => {
        // Don't reset - we want to load existing data
      });

    document
      .getElementById("projectEditWizardModal")
      ?.addEventListener("shown.bs.modal", () => {
        this.initializeEditWizard();
      });

    document
      .getElementById("projectEditWizardModal")
      ?.addEventListener("hidden.bs.modal", () => {
        this.resetEditWizard();
      });

    // Navigation buttons
    document.getElementById("editNextStepBtn")?.addEventListener("click", () => {
      this.nextStep();
    });

    document.getElementById("editPrevStepBtn")?.addEventListener("click", () => {
      this.previousStep();
    });

    document
      .getElementById("editFinalizeProjectBtn")
      ?.addEventListener("click", () => {
        this.finalizeProject();
      });

    // Save as Draft button
    document
      .getElementById("editSaveAsDraftBtn")
      ?.addEventListener("click", () => {
        this.saveAsDraft();
      });

    // Form validation on input change
    document
      .querySelectorAll(
        "#projectEditForm input, #projectEditForm select, #projectEditForm textarea"
      )
      .forEach((element) => {
        element.addEventListener("change", () => {
          this.validateCurrentStep();
        });
      });

    // Add Phase button
    document.getElementById("editAddPhaseBtn")?.addEventListener("click", () => {
      const phasesContainer = document.getElementById("editPhasesContainer");
      if (phasesContainer) {
        phasesContainer.appendChild(this.buildPhaseRow());
      }
    });

    // Add Task button
    document.getElementById("editAddTaskBtn")?.addEventListener("click", () => {
      const tasksContainer = document.getElementById("editTasksContainer");
      if (tasksContainer) {
        tasksContainer.appendChild(this.buildTaskRow());
      }
    });
  }

  async initializeEditWizard() {
    // Load clients and contractors first
    await this.loadClients();
    await this.loadContractors();
    
    // Get the project ID from the modal data attribute
    const modal = document.getElementById("projectEditWizardModal");
    const projectId = modal?.getAttribute("data-project-id");
    
    if (projectId) {
      console.log("Loading project for editing:", projectId);
      await this.loadProjectForEditing(projectId);
    } else {
      console.error("No project ID provided for editing");
    }
  }

  async loadProjectForEditing(projectId) {
    try {
      console.log("Loading project data:", projectId);
      
      // Load project basic data
      const projectResponse = await fetch(`/ProjectManager/GetProject/${projectId}`);
      if (projectResponse.ok) {
        const projectData = await projectResponse.json();
        console.log("Received project data:", projectData);
        
        this.projectId = projectId;
        this.isEditingDraft = true;
        
        // Populate form fields
        this.populateFormWithProjectData(projectData);
        
        // Load phases and tasks
        await this.loadProjectPhases(projectId);
        await this.loadProjectTasks(projectId);
        
        console.log("Successfully loaded project for editing:", projectId);
      } else {
        console.error("Failed to load project:", projectResponse.status);
        this.showError("Failed to load project data");
      }
    } catch (error) {
      console.error("Error loading project:", error);
      this.showError("Error loading project: " + error.message);
    }
  }

  populateFormWithProjectData(projectData) {
    console.log("Populating form with project data:", projectData);
    
    // Populate basic information
    if (projectData.name) {
      const nameField = document.getElementById("editProjectName");
      if (nameField) {
        nameField.value = projectData.name;
        console.log("Set project name:", projectData.name);
      }
    }
    
    if (projectData.description) {
      const descField = document.getElementById("editProjectDescription");
      if (descField) {
        descField.value = projectData.description;
        console.log("Set project description:", projectData.description);
      }
    }
    
    if (projectData.clientId) {
      const clientField = document.getElementById("editClientSelect");
      if (clientField) {
        clientField.value = projectData.clientId;
        console.log("Set client ID:", projectData.clientId);
      }
    }
    
    if (projectData.budgetPlanned) {
      const budgetField = document.getElementById("editBudgetPlanned");
      if (budgetField) {
        budgetField.value = projectData.budgetPlanned;
        console.log("Set budget:", projectData.budgetPlanned);
      }
    }
    
    if (projectData.startDate) {
      const startField = document.getElementById("editStartDate");
      if (startField) {
        startField.value = new Date(projectData.startDate).toISOString().split('T')[0];
        console.log("Set start date:", projectData.startDate);
      }
    }
    
    if (projectData.endDatePlanned) {
      const endField = document.getElementById("editEndDatePlanned");
      if (endField) {
        endField.value = new Date(projectData.endDatePlanned).toISOString().split('T')[0];
        console.log("Set end date:", projectData.endDatePlanned);
      }
    }
  }

  async loadProjectPhases(projectId) {
    try {
      console.log("Loading phases for project:", projectId);
      const response = await fetch(`/ProjectManager/GetProjectPhases?id=${projectId}`);
      if (response.ok) {
        const phases = await response.json();
        console.log("Received phases:", phases);
        
        if (phases && phases.length > 0) {
          const phasesContainer = document.getElementById("editPhasesContainer");
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
          const tasksContainer = document.getElementById("editTasksContainer");
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

  buildPhaseOptionsHtml(selectedPhaseId = "") {
    const phases = Array.from(document.querySelectorAll(".phase-name")).map(
      (input) => ({
        id: input.dataset.id,
        name: input.value || "Untitled Phase",
      })
    );
    return [
      `<option value="">Select phase...</option>`,
      ...phases.map(
        (p) =>
          `<option value="${p.id}" ${
            p.id === selectedPhaseId ? "selected" : ""
          }>${p.name}</option>`
      ),
    ].join("");
  }

  nextStep() {
    if (this.validateCurrentStep()) {
      this.saveCurrentStepData();

      if (this.currentStep < this.totalSteps) {
        this.currentStep++;
        this.updateWizardDisplay();
        
        // Auto-save after moving to next step
        this.autoSaveAsDraft();
      }
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.updateWizardDisplay();
    }
  }

  updateWizardDisplay() {
    // Update progress bar
    const progress = (this.currentStep / this.totalSteps) * 100;
    const progressBar = document.getElementById("editWizardProgress");
    if (progressBar) {
      progressBar.style.width = `${progress}%`;
    }

    // Update step counter and title
    const stepTitles = [
      "Basic Information",
      "Budget & Timeline",
      "Phases",
      "Tasks & Contractors",
      "Review & Finalize",
    ];
    document.getElementById("editStepTitle").textContent =
      stepTitles[this.currentStep - 1];

    // Show/hide steps
    for (let i = 1; i <= this.totalSteps; i++) {
      const stepElement = document.getElementById(`editStep${i}`);
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

    // Update review step if we're on step 5
    if (this.currentStep === 5) {
      this.updateReviewStep();
    }
  }

  updateNavigationButtons() {
    const prevBtn = document.getElementById("editPrevStepBtn");
    const nextBtn = document.getElementById("editNextStepBtn");
    const finalizeBtn = document.getElementById("editFinalizeProjectBtn");

    if (prevBtn) {
      prevBtn.style.display = this.currentStep > 1 ? "inline-block" : "none";
    }

    if (nextBtn && finalizeBtn) {
      if (this.currentStep === this.totalSteps) {
        nextBtn.style.display = "none";
        finalizeBtn.style.display = "inline-block";
      } else {
        nextBtn.style.display = "inline-block";
        finalizeBtn.style.display = "none";
      }
    }
  }

  updateReviewStep() {
    const form = document.getElementById("projectEditForm");
    if (!form) return;

    // Update basic info
    document.getElementById("editReviewName").textContent = 
      form.querySelector("#editProjectName")?.value || "-";
    document.getElementById("editReviewClient").textContent = 
      this.getClientName(form.querySelector("#editClientSelect")?.value) || "-";
    document.getElementById("editReviewBudget").textContent = 
      form.querySelector("#editBudgetPlanned")?.value || "-";
    document.getElementById("editReviewStartDate").textContent = 
      form.querySelector("#editStartDate")?.value || "-";
    document.getElementById("editReviewEndDate").textContent = 
      form.querySelector("#editEndDatePlanned")?.value || "-";
    document.getElementById("editReviewDescription").textContent = 
      form.querySelector("#editProjectDescription")?.value || "-";

    // Update phases and tasks
    this.updateReviewPhaseTaskGroups();
  }

  updateReviewPhaseTaskGroups() {
    const groupsContainer = document.getElementById("editReviewPhaseTaskGroups");
    if (!groupsContainer) return;

    const phases = Array.from(document.querySelectorAll(".phase-name")).map(
      (input) => ({
        id: input.dataset.id,
        name: input.value || "Untitled Phase",
      })
    );

    groupsContainer.innerHTML = "";

    phases.forEach((p, idx) => {
      const card = document.createElement("div");
      card.className = "card mb-2";
      const tasks = Array.from(
        document.querySelectorAll(".task-phase-select")
      ).filter((select) => select.value === p.id);
      const listItems = tasks.map((select) => {
        const taskRow = select.closest(".card-body");
        const taskName = taskRow?.querySelector(".task-name")?.value || "Untitled Task";
        return `<li class="list-group-item">${taskName}</li>`;
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

  getClientName(clientId) {
    const client = this.clients.find(c => c.userId === clientId);
    return client ? client.fullName : null;
  }

  validateCurrentStep() {
    const form = document.getElementById("projectEditForm");
    if (!form) return false;

    switch (this.currentStep) {
      case 1:
        const name = form.querySelector("#editProjectName")?.value?.trim();
        const client = form.querySelector("#editClientSelect")?.value;
        if (!name) {
          this.showError("Project name is required.");
          return false;
        }
        if (!client) {
          this.showError("Please select a client.");
          return false;
        }
        break;
      case 2:
        const budget = parseFloat(form.querySelector("#editBudgetPlanned")?.value);
        const startDate = form.querySelector("#editStartDate")?.value;
        const endDate = form.querySelector("#editEndDatePlanned")?.value;
        if (!budget || budget <= 0) {
          this.showError("Budget must be greater than 0.");
          return false;
        }
        if (!startDate) {
          this.showError("Start date is required.");
          return false;
        }
        if (!endDate) {
          this.showError("End date is required.");
          return false;
        }
        if (new Date(startDate) >= new Date(endDate)) {
          this.showError("End date must be after start date.");
          return false;
        }
        break;
    }
    return true;
  }

  saveCurrentStepData() {
    // Store current step data
    const form = document.getElementById("projectEditForm");
    if (!form) return;

    this.formData = {
      ...this.formData,
      name: form.querySelector("#editProjectName")?.value || "",
      description: form.querySelector("#editProjectDescription")?.value || "",
      clientId: form.querySelector("#editClientSelect")?.value || "",
      budgetPlanned: parseFloat(form.querySelector("#editBudgetPlanned")?.value) || 0,
      startDate: form.querySelector("#editStartDate")?.value || "",
      endDatePlanned: form.querySelector("#editEndDatePlanned")?.value || "",
    };
  }

  collectProjectData() {
    const form = document.getElementById("projectEditForm");
    if (!form) {
      console.error("Project edit form not found");
      return {
        projectId: this.projectId,
        name: "",
        description: "",
        clientId: "",
        startDate: null,
        endDatePlanned: null,
        budgetPlanned: 0,
        // status will be set by the calling function
      };
    }

    return {
      projectId: this.projectId,
      name: form.querySelector("#editProjectName")?.value || "",
      description: form.querySelector("#editProjectDescription")?.value || "",
      clientId: form.querySelector("#editClientSelect")?.value || "",
      startDate: this.parseDateInput(form.querySelector("#editStartDate")?.value),
      endDatePlanned: this.parseDateInput(
        form.querySelector("#editEndDatePlanned")?.value
      ),
      budgetPlanned:
        parseFloat(form.querySelector("#editBudgetPlanned")?.value) || 0,
      // status will be set by the calling function
    };
  }

  collectPhases() {
    const phases = [];
    document.querySelectorAll(".phase-name").forEach((input) => {
      const row = input.closest(".card-body");
      if (row) {
        const phase = {
          phaseId: input.dataset.id,
          name: input.value?.trim(),
          description: row.querySelector(".phase-desc")?.value?.trim() || "",
          budget: parseFloat(row.querySelector(".phase-budget")?.value) || 0,
          startDate: this.parseDateInput(row.querySelector(".phase-start")?.value),
          endDate: this.parseDateInput(row.querySelector(".phase-end")?.value),
        };
        if (phase.name) {
          phases.push(phase);
        }
      }
    });
    return phases;
  }

  collectTasks() {
    const tasks = [];
    document.querySelectorAll(".task-name").forEach((input) => {
      const row = input.closest(".card-body");
      if (row) {
        const task = {
          taskId: input.dataset.id,
          name: input.value?.trim(),
          phaseId: row.querySelector(".task-phase-select")?.value || "",
          contractorId: row.querySelector(".task-contractor")?.value || "",
          budget: parseFloat(row.querySelector(".task-budget")?.value) || 0,
          dueDate: this.parseDateInput(row.querySelector(".task-due")?.value),
          startDate: this.parseDateInput(row.querySelector(".task-start")?.value),
          description: row.querySelector(".task-desc")?.value?.trim() || "",
        };
        if (task.name) {
          tasks.push(task);
        }
      }
    });
    return tasks;
  }

  parseDateInput(dateString) {
    if (!dateString) return null;
    const date = new Date(dateString);
    return isNaN(date.getTime()) ? null : date.toISOString();
  }

  async saveAsDraft() {
    console.log("saveAsDraft called");
    
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

      // Use the complete project update endpoint
      const completeRequest = {
        project: projectData,
        phases: phases,
        tasks: tasks,
      };

      console.log("Saving as draft:", completeRequest);
      const createRes = await fetch(`/ProjectManager/SaveProject`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(completeRequest),
      });

      if (createRes.ok) {
        const data = await createRes.json();
        if (data?.success) {
          this.showSuccess("Project saved as draft successfully!");
          
          // Close the modal after successful save
          setTimeout(() => {
            const modal = bootstrap.Modal.getInstance(document.getElementById("projectEditWizardModal"));
            if (modal) {
              modal.hide();
            }
            // Reload the page to show the updated draft
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
    console.log("finalizeProject called");
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

      // Use the complete project update endpoint
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

      if (createRes.ok) {
        const data = await createRes.json();
        console.log("Response data:", data);
        
        if (data?.success) {
          this.showSuccess("Project finalized successfully!");
          
          // Close the modal after successful save
          setTimeout(() => {
            const modal = bootstrap.Modal.getInstance(document.getElementById("projectEditWizardModal"));
            if (modal) {
              modal.hide();
            }
            // Reload the page to show the finalized project
            window.location.reload();
          }, 1500);
        } else {
          this.showError("Failed to finalize project: " + (data?.message || "Unknown error"));
        }
      } else {
        console.error("HTTP Error:", createRes.status, createRes.statusText);
        this.showError(
          `Server error: ${createRes.status} ${createRes.statusText}`
        );
      }
    } catch (error) {
      console.error("Finalize project failed:", error);
      this.showError("Project finalization failed: " + error.message);
    }
  }

  async loadClients() {
    try {
      const response = await fetch("/ProjectManager/GetClients");
      if (response.ok) {
        this.clients = await response.json();
        this.populateClientSelect();
      } else {
        this.showError("Failed to load clients. Please refresh the page.");
      }
    } catch (error) {
      console.error("Error loading clients:", error);
      this.showError("Error loading clients. Please check your connection.");
    }
  }

  async loadContractors() {
    try {
      const response = await fetch("/ProjectManager/GetContractors");
      if (response.ok) {
        this.contractors = await response.json();
      } else {
        console.warn("Failed to load contractors");
      }
    } catch (error) {
      console.warn("Error loading contractors:", error);
    }
  }

  populateClientSelect() {
    const clientSelect = document.getElementById("editClientSelect");
    if (clientSelect && this.clients) {
      // Clear existing options except the first one
      clientSelect.innerHTML = '<option value="">Select a client...</option>';
      
      this.clients.forEach(client => {
        const option = document.createElement("option");
        option.value = client.userId;
        option.textContent = `${client.fullName} (${client.email})`;
        clientSelect.appendChild(option);
      });
    }
  }

  setupFormValidation() {
    // Add any additional form validation setup here
  }

  resetEditWizard() {
    this.currentStep = 1;
    this.formData = {};
    this.projectId = null;
    this.isEditingDraft = false;

    // Reset form
    const form = document.getElementById("projectEditForm");
    if (form) {
      form.reset();
    }

    // Clear phases and tasks containers
    const phasesContainer = document.getElementById("editPhasesContainer");
    if (phasesContainer) {
      phasesContainer.innerHTML = "";
    }

    const tasksContainer = document.getElementById("editTasksContainer");
    if (tasksContainer) {
      tasksContainer.innerHTML = "";
    }

    // Reset progress
    this.updateWizardDisplay();
    this.updateNavigationButtons();

    // Clear any error messages
    this.clearErrors();
  }

  clearErrors() {
    // Remove any existing error messages
    const existingErrors = document.querySelectorAll('.alert-danger');
    existingErrors.forEach(error => error.remove());
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

  async autoSaveAsDraft() {
    try {
      console.log("🔥 EDIT WIZARD: Auto-saving as draft after step", this.currentStep);
      
      // Collect all project data
      const projectData = this.collectProjectData();
      const phases = this.collectPhases().filter((p) => p.name?.trim());
      const tasks = this.collectTasks().filter((t) => t.name?.trim());

      // Only save if we have basic project info
      if (!projectData.name || !projectData.clientId) {
        console.log("🔥 EDIT WIZARD: Skipping auto-save - missing required fields");
        return;
      }

      // Set project status to Draft
      projectData.status = "Draft";
      projectData.projectId = this.projectId; // Include the project ID for updates

      const completeRequest = {
        project: projectData,
        phases: phases,
        tasks: tasks,
      };

      console.log("🔥 EDIT WIZARD: Auto-saving request:", completeRequest);

      const response = await fetch("/ProjectManager/SaveProject", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(completeRequest),
      });

      if (response.ok) {
        const result = await response.json();
        console.log("🔥 EDIT WIZARD: Auto-save successful:", result);
        
        // Update the project ID if this was a new project
        if (result.projectId && !this.projectId) {
          this.projectId = result.projectId;
        }
      } else {
        console.warn("🔥 EDIT WIZARD: Auto-save failed:", response.status);
      }
    } catch (error) {
      console.warn("🔥 EDIT WIZARD: Auto-save error:", error);
    }
  }
}

// Initialize the edit wizard when the page loads
document.addEventListener('DOMContentLoaded', function() {
  window.projectEditWizard = new ProjectEditWizard();
});
