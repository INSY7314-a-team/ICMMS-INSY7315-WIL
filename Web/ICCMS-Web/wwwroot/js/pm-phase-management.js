// PM Phase Management JavaScript
document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 PM Phase Management Initialized");

  initializePhaseActions();
  initializePhaseForm();

  console.log("✅ PM Phase Management components initialized");
});

function initializePhaseActions() {
  // Edit phase buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".edit-phase-btn")) {
      const button = e.target.closest(".edit-phase-btn");
      const phaseId = button.getAttribute("data-phase-id");
      editPhase(phaseId);
    }
  });

  // Delete phase buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".delete-phase-btn")) {
      const button = e.target.closest(".delete-phase-btn");
      const phaseId = button.getAttribute("data-phase-id");
      deletePhase(phaseId);
    }
  });
}

function initializePhaseForm() {
  const addPhaseBtn = document.getElementById("addPhaseBtn");
  if (addPhaseBtn) {
    addPhaseBtn.addEventListener("click", function () {
      resetPhaseForm();
    });
  }
}

function editPhase(phaseId) {
  console.log("Edit phase:", phaseId);

  // Show loading state
  showInfoMessage("Loading phase details...");

  // In a real implementation, you would fetch the phase details
  // For now, we'll simulate loading
  setTimeout(() => {
    // Populate the form with phase data
    populatePhaseForm({
      PhaseId: phaseId,
      Name: "Sample Phase Name",
      Description: "Sample phase description",
      Status: "Active",
      StartDate: "2024-01-01",
      EndDate: "2024-01-31",
      Budget: 50000,
      AssignedTo: "contractor-id-1",
    });

    // Show the modal
    const phaseModal = document.getElementById("phaseFormModal");
    if (phaseModal) {
      const modal = new bootstrap.Modal(phaseModal);
      modal.show();
    }
  }, 500);
}

function deletePhase(phaseId) {
  console.log("Delete phase:", phaseId);

  if (
    !confirm(
      "Are you sure you want to delete this phase? This action cannot be undone."
    )
  ) {
    return;
  }

  // Show loading state
  showInfoMessage("Deleting phase...");

  // Simulate API call
  setTimeout(() => {
    // Remove the phase card from the UI
    const phaseCard = document.querySelector(`[data-phase-id="${phaseId}"]`);
    if (phaseCard) {
      phaseCard.remove();
    }

    showSuccessMessage("Phase deleted successfully");

    // Refresh phases list
    if (typeof window.refreshPhasesList === "function") {
      window.refreshPhasesList();
    }
  }, 1000);
}

function populatePhaseForm(phaseData) {
  // Populate form fields
  document.getElementById("phaseId").value = phaseData.PhaseId || "";
  document.getElementById("phaseName").value = phaseData.Name || "";
  document.getElementById("phaseDescription").value =
    phaseData.Description || "";
  document.getElementById("phaseStatus").value = phaseData.Status || "Planning";
  document.getElementById("phaseStartDate").value = phaseData.StartDate || "";
  document.getElementById("phaseEndDate").value = phaseData.EndDate || "";
  document.getElementById("phaseBudget").value = phaseData.Budget || "";
  document.getElementById("phaseAssignedTo").value = phaseData.AssignedTo || "";

  // Update modal title and button text
  document.getElementById("phaseModalTitle").textContent = "Edit Phase";
  document.getElementById("savePhaseBtnText").textContent = "Update Phase";
}

function resetPhaseForm() {
  // Clear form fields
  document.getElementById("phaseId").value = "";
  document.getElementById("phaseName").value = "";
  document.getElementById("phaseDescription").value = "";
  document.getElementById("phaseStatus").value = "Planning";
  document.getElementById("phaseBudget").value = "";
  document.getElementById("phaseAssignedTo").value = "";

  // Set default dates
  const today = new Date().toISOString().split("T")[0];
  const nextWeek = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
    .toISOString()
    .split("T")[0];
  document.getElementById("phaseStartDate").value = today;
  document.getElementById("phaseEndDate").value = nextWeek;

  // Update modal title and button text
  document.getElementById("phaseModalTitle").textContent = "Add Phase";
  document.getElementById("savePhaseBtnText").textContent = "Save Phase";
}

// Global functions for use by other scripts
window.editPhase = editPhase;
window.deletePhase = deletePhase;
window.resetPhaseForm = resetPhaseForm;
window.populatePhaseForm = populatePhaseForm;

// Utility functions
function showSuccessMessage(message) {
  if (typeof window.PMProjectDetail?.showSuccessMessage === "function") {
    window.PMProjectDetail.showSuccessMessage(message);
  } else {
    console.log("Success:", message);
  }
}

function showErrorMessage(message) {
  if (typeof window.PMProjectDetail?.showErrorMessage === "function") {
    window.PMProjectDetail.showErrorMessage(message);
  } else {
    console.error("Error:", message);
  }
}

function showInfoMessage(message) {
  if (typeof window.PMProjectDetail?.showInfoMessage === "function") {
    window.PMProjectDetail.showInfoMessage(message);
  } else {
    console.log("Info:", message);
  }
}
