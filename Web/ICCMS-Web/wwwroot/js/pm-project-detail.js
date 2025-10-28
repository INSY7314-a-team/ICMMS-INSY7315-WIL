// PM Project Detail Page - Main Coordinator
document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 PM Project Detail Page Initialized");

  // Initialize all components
  initializeTabs();
  initializeModals();
  initializeRefreshHandlers();
  initializeFloatingActionButton();

  console.log("✅ PM Project Detail components initialized");
});

// Initialize floating action button
function initializeFloatingActionButton() {
  const fabBtn = document.querySelector('.blueprint-fab-btn');
  if (fabBtn) {
    fabBtn.addEventListener('click', function(e) {
      e.preventDefault();
      openBlueprintProcessing();
    });
  }
}

// Open blueprint processing workflow
function openBlueprintProcessing() {
  const projectId = getProjectIdFromUrl();
  if (!projectId) {
    console.error('No project ID found');
    return;
  }

  console.log('Opening blueprint processing for project:', projectId);
  
  // Check if openBlueprintPicker function exists (from modal)
  if (typeof openBlueprintPicker === 'function') {
    openBlueprintPicker(projectId, function(url) {
      console.log('Blueprint URL selected:', url);
      processBlueprintAndOpen(projectId, url);
    });
  } else {
    console.error('openBlueprintPicker function not found');
    alert('Blueprint processing functionality is not available. Please ensure all required scripts are loaded.');
  }
}

// Get project ID from URL
function getProjectIdFromUrl() {
  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get('projectId');
}

// Process blueprint and open estimate editor (fallback function)
function processBlueprintAndOpen(projectId, blueprintUrl) {
  console.log('Processing blueprint:', blueprintUrl, 'for project:', projectId);
  
  // This would typically call the API to process the blueprint
  // For now, we'll just show a message and then open the estimate editor
  if (typeof showSuccessMessage === 'function') {
    showSuccessMessage('Blueprint processing initiated!');
  } else {
    alert('Blueprint processing initiated!');
  }
  
  // After processing, open the estimate editor
  setTimeout(() => {
    if (typeof openEstimateEditor === 'function') {
      console.log('Opening estimate editor for project:', projectId);
      openEstimateEditor(projectId);
    } else {
      console.error('openEstimateEditor function not found');
      alert('Estimate editor is not available. Please ensure all required scripts are loaded.');
    }
  }, 1000);
}

// Open invoice editor
function openInvoiceEditor(projectId, invoiceId) {
  console.log('Opening invoice editor for project:', projectId, 'invoice:', invoiceId);
  
  // Call the global function from the modal (different name to avoid recursion)
  if (typeof window.openInvoiceEditorModal === 'function') {
    window.openInvoiceEditorModal(projectId, invoiceId);
  } else {
    console.error('Invoice editor modal function not found');
    alert('Invoice editor is not available. Please ensure all required scripts are loaded.');
  }
}

function initializeTabs() {
  const tabButtons = document.querySelectorAll(
    '#approvalTabs button[data-bs-toggle="tab"]'
  );

  tabButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const targetTab = this.getAttribute("data-bs-target");
      console.log("Switching to tab:", targetTab);

      // Update active states
      tabButtons.forEach((btn) => btn.classList.remove("active"));
      this.classList.add("active");

      // Show/hide content
      const tabPanes = document.querySelectorAll(
        "#approvalTabsContent .tab-pane"
      );
      tabPanes.forEach((pane) => {
        pane.classList.remove("show", "active");
      });

      const targetPane = document.querySelector(targetTab);
      if (targetPane) {
        targetPane.classList.add("show", "active");
      }
    });
  });
}

function initializeModals() {
  // Initialize Bootstrap modals
  const modals = [
    "phaseFormModal",
    "taskFormModal",
    "progressReportReviewModal",
    "taskCompletionReviewModal",
    "rejectionNotesModal",
    "completionRejectionModal",
  ];

  modals.forEach((modalId) => {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
      try {
        // Ensure modal is properly initialized
        new bootstrap.Modal(modalElement);
        console.log(`✅ Modal ${modalId} initialized successfully`);
      } catch (error) {
        console.warn(`⚠️ Failed to initialize modal ${modalId}:`, error);
      }
    } else {
      console.warn(`⚠️ Modal element ${modalId} not found`);
    }
  });
}

function initializeRefreshHandlers() {
  // Add refresh buttons if they exist
  const refreshButtons = document.querySelectorAll('[id$="RefreshBtn"]');

  refreshButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const buttonId = this.id;
      console.log("Refresh button clicked:", buttonId);

      // Show loading state
      const originalText = this.innerHTML;
      this.innerHTML =
        '<i class="fa-solid fa-spinner fa-spin me-1"></i>Refreshing...';
      this.disabled = true;

      // Simulate refresh (in real implementation, this would reload data)
      setTimeout(() => {
        this.innerHTML = originalText;
        this.disabled = false;
        showSuccessMessage("Data refreshed successfully");
      }, 1000);
    });
  });
}

// Global refresh functions that can be called by other scripts
window.refreshPhasesList = function () {
  console.log("Refreshing phases list...");
  // This would typically reload the phases section
  // For now, just show a success message
  showSuccessMessage("Phases list refreshed");
};

window.refreshTasksList = function () {
  console.log("Refreshing tasks list...");
  // This would typically reload the tasks section
  // For now, just show a success message
  showSuccessMessage("Tasks list refreshed");
};

window.refreshApprovalsList = function () {
  console.log("Refreshing approvals list...");
  // This would typically reload the approvals section
  // For now, just show a success message
  showSuccessMessage("Approvals list refreshed");
};

// Utility functions
function showSuccessMessage(message) {
  const notification = document.createElement("div");
  notification.className = "alert alert-success position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-check-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;
  document.body.appendChild(notification);
  setTimeout(() => notification.remove(), 3000);
}

function showErrorMessage(message) {
  const notification = document.createElement("div");
  notification.className = "alert alert-danger position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-exclamation-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;
  document.body.appendChild(notification);
  setTimeout(() => notification.remove(), 5000);
}

function showInfoMessage(message) {
  const notification = document.createElement("div");
  notification.className = "alert alert-info position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-info-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;
  document.body.appendChild(notification);
  setTimeout(() => notification.remove(), 4000);
}

// Export functions for use by other scripts
window.PMProjectDetail = {
  showSuccessMessage,
  showErrorMessage,
  showInfoMessage,
  refreshPhasesList,
  refreshTasksList,
  refreshApprovalsList,
};
