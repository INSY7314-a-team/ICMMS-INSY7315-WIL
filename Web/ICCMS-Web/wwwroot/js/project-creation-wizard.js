/**
 * Project Creation Wizard JavaScript
 * Handles the multi-step project creation process
 */

class ProjectCreationWizard {
  constructor() {
    this.currentStep = 1;
    this.totalSteps = 4;
    this.formData = {};
    this.clients = [];

    this.init();
  }

  init() {
    this.bindEvents();
    this.loadClients();
    this.setupFormValidation();
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
        this.createProject();
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
      "Project Details",
      "Review & Create",
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
    if (this.currentStep === 4) {
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

  async createProject() {
    if (!this.validateCurrentStep()) {
      return;
    }

    this.showLoading(true);

    try {
      // Prepare project data
      const projectData = {
        ...this.formData,
        ProjectId: this.generateProjectId(),
        StartDate: new Date(this.formData.StartDate).toISOString(),
        EndDatePlanned: new Date(this.formData.EndDatePlanned).toISOString(),
        BudgetPlanned: parseFloat(this.formData.BudgetPlanned) || 0,
        CompletionPhase: parseInt(this.formData.CompletionPhase) || 1,
        Priority: this.formData.Priority || "Medium",
        Notes: this.formData.Notes || "",
      };

      // Determine if saving as draft
      const saveAsDraft =
        document.getElementById("saveAsDraft")?.checked || false;
      if (saveAsDraft) {
        projectData.Status = "Draft";
      }

      // Make API call
      const endpoint = saveAsDraft
        ? "/ProjectManager/SaveDraftProject"
        : "/ProjectManager/CreateProjectAjax";
      const response = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(projectData),
      });

      const result = await response.json();

      if (result.success) {
        this.showSuccess(
          `Project ${saveAsDraft ? "saved as draft" : "created"} successfully!`
        );
        this.closeWizard();
        // Refresh the page to show the new project
        setTimeout(() => window.location.reload(), 1500);
      } else {
        this.showError(result.error || "Failed to create project.");
      }
    } catch (error) {
      console.error("Error creating project:", error);
      this.showError("An unexpected error occurred. Please try again.");
    } finally {
      this.showLoading(false);
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
    new ProjectCreationWizard();
  }
});
