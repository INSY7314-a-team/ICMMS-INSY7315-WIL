document.addEventListener("DOMContentLoaded", function () {
  const quotationRejectionModal = new bootstrap.Modal(
    document.getElementById("quotationRejectionModal")
  );
  let currentQuotationId = null;
  let currentTaskId = null;
  let currentPhaseId = null;

  // Handle showing the rejection modal
  document.body.addEventListener("click", function (e) {
    if (e.target.matches(".reject-quote-btn")) {
      currentQuotationId = e.target.dataset.quotationId;
      document.getElementById("rejectionQuotationId").value =
        currentQuotationId;
      quotationRejectionModal.show();
    }
  });

  // Handle quotation approval
  document.body.addEventListener("click", async function (e) {
    if (e.target.matches(".approve-quote-btn")) {
      const quotationId = e.target.dataset.quotationId;
      if (!quotationId) return;

      if (!confirm("Are you sure you want to approve this quotation?")) {
        return;
      }

      const button = e.target;
      const originalText = button.innerHTML;
      button.disabled = true;
      button.innerHTML =
        '<span class="spinner-border spinner-border-sm me-1"></span>Approving...';

      try {
        const response = await fetch("/Clients/ApproveQuotation", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ quotationId: quotationId }),
        });

        const result = await response.json();

        if (result.success) {
          alert("Quotation approved successfully.");
          location.reload();
        } else {
          alert(
            "Failed to approve quotation: " + (result.error || "Unknown error")
          );
          button.disabled = false;
          button.innerHTML = originalText;
        }
      } catch (error) {
        console.error("Error approving quotation:", error);
        alert("An error occurred. Please try again.");
        button.disabled = false;
        button.innerHTML = originalText;
      }
    }
  });

  // Handle submitting the rejection reason
  document
    .getElementById("submitRejectionBtn")
    .addEventListener("click", async function () {
      const reason = document.getElementById("rejectionReason").value;
      if (!reason) {
        alert("Rejection reason is required.");
        return;
      }

      const payload = {
        quotationId: currentQuotationId,
        reason: reason,
      };

      try {
        const response = await fetch("/Clients/RejectQuotation", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        const result = await response.json();

        if (result.success) {
          alert("Quotation rejected successfully.");
          quotationRejectionModal.hide();
          location.reload();
        } else {
          alert(
            "Failed to reject quotation: " + (result.error || "Unknown error")
          );
        }
      } catch (error) {
        console.error("Error rejecting quotation:", error);
        alert("An error occurred. Please try again.");
      }
    });

  // Handle task details button clicks
  document.body.addEventListener("click", function (e) {
    if (
      e.target.closest(".view-phases-button") &&
      e.target.closest("[data-task-id]")
    ) {
      const button = e.target.closest(".view-phases-button");
      const taskId = button.getAttribute("data-task-id");
      currentTaskId = taskId;
      loadTaskDetails(taskId);
    }
  });

  // Handle phase details button clicks
  document.body.addEventListener("click", function (e) {
    if (
      e.target.closest(".view-phases-button") &&
      e.target.closest("[data-phase-id]")
    ) {
      const button = e.target.closest(".view-phases-button");
      const phaseId = button.getAttribute("data-phase-id");
      currentPhaseId = phaseId;
      loadPhaseDetails(phaseId);
    }
  });

  function loadTaskDetails(taskId) {
    const content = document.getElementById("taskDetailsContent");

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading task details...</p>
      </div>
    `;

    // Find the task data from the global data
    const task = window.projectData?.tasks?.find(
      (t) => t.taskId == taskId || t.taskId === taskId
    );

    if (task) {
      content.innerHTML = generateTaskDetailsContent(task);
    } else {
      content.innerHTML = `
        <div class="alert alert-danger">
          <i class="fa-solid fa-exclamation-triangle me-2"></i>
          Task not found. ID: ${taskId}
          <br><small>Available task IDs: ${
            window.projectData?.tasks?.map((t) => t.taskId).join(", ") || "None"
          }</small>
        </div>
      `;
    }
  }

  function loadPhaseDetails(phaseId) {
    const content = document.getElementById("phaseDetailsContent");

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading phase details...</p>
      </div>
    `;

    // Find the phase data from the global data
    const phase = window.projectData?.phases?.find(
      (p) => p.phaseId == phaseId || p.phaseId === phaseId
    );

    if (phase) {
      content.innerHTML = generatePhaseDetailsContent(phase);
    } else {
      content.innerHTML = `
        <div class="alert alert-danger">
          <i class="fa-solid fa-exclamation-triangle me-2"></i>
          Phase not found. ID: ${phaseId}
          <br><small>Available phase IDs: ${
            window.projectData?.phases?.map((p) => p.phaseId).join(", ") ||
            "None"
          }</small>
        </div>
      `;
    }
  }

  function generateTaskDetailsContent(task) {
    const startDate = new Date(task.startDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const dueDate = new Date(task.dueDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const phaseName =
      window.projectData?.phases?.find((p) => p.phaseId === task.phaseId)
        ?.name || "Unknown Phase";
    const contractorName =
      window.projectData?.contractors?.find((c) => c.userId === task.assignedTo)
        ?.fullName || "Unassigned";

    return `
      <div class="task-details-container">
        <!-- Task Header -->
        <div class="row mb-4">
          <div class="col-md-8">
            <h4 class="mb-2 text-white">${task.name}</h4>
            <p class="text-muted mb-0">Task for project</p>
          </div>
          <div class="col-md-4 text-end">
            <div class="mb-2">
              <span class="badge ${getTaskStatusBadgeClass(
                task.status
              )} me-2">${task.status}</span>
              <span class="badge ${getPriorityBadgeClass(
                task.priority
              )} me-2">${task.priority}</span>
            </div>
            <small class="text-muted">Phase: ${phaseName}</small>
          </div>
        </div>
        
        <!-- Task Summary -->
        <div class="row mb-4">
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-calendar-alt me-2" style="color: #0dcaf0;"></i>Dates
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold text-white">${startDate} - ${dueDate}</div>
                <small class="text-muted">Start Date - Due Date</small>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-chart-line me-2" style="color: #f7ec59;"></i>Progress
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold fs-4" style="color: #f7ec59;">${
                  task.progress
                }%</div>
                <small class="text-muted">Overall task progress</small>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Description -->
        <div class="row mb-4">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-align-left me-2" style="color: #f7ec59;"></i>Description
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <p class="text-white">${
                  task.description || "No description provided."
                }</p>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Additional Information -->
        <div class="row">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-info-circle me-2" style="color: #f7ec59;"></i>Additional Information
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-md-6">
                    <small class="text-muted">Task ID</small>
                    <div class="fw-bold font-monospace text-white">${
                      task.taskId
                    }</div>
                  </div>
                  <div class="col-md-6">
                    <small class="text-muted">Assigned To</small>
                    <div class="fw-bold text-white">${contractorName}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  function generatePhaseDetailsContent(phase) {
    const startDate = new Date(phase.startDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const endDate = new Date(phase.endDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const progress = phase.progress || 0;

    return `
      <div class="phase-details-container">
        <!-- Phase Header -->
        <div class="row mb-4">
          <div class="col-md-8">
            <h4 class="mb-2 text-white">${phase.name}</h4>
            <p class="text-muted mb-0">Phase for project</p>
          </div>
          <div class="col-md-4 text-end">
            <div class="mb-2">
              <span class="badge ${getStatusBadgeClass(
                phase.status
              )} me-2">${phase.status}</span>
            </div>
            <small class="text-muted">Created: ${new Date(
              phase.createdAt || new Date()
            ).toLocaleDateString("en-US", {
              year: "numeric",
              month: "short",
              day: "numeric",
            })}</small>
          </div>
        </div>
        
        <!-- Phase Summary -->
        <div class="row mb-4">
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-calendar-alt me-2" style="color: #0dcaf0;"></i>Dates
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold text-white">${startDate} - ${endDate}</div>
                <small class="text-muted">Start Date - End Date</small>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-chart-line me-2" style="color: #f7ec59;"></i>Progress
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold fs-4" style="color: #f7ec59;">${progress}%</div>
                <small class="text-muted">Overall phase progress</small>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Description -->
        <div class="row mb-4">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-align-left me-2" style="color: #f7ec59;"></i>Description
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <p class="text-white">${
                  phase.description || "No description provided."
                }</p>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Additional Information -->
        <div class="row">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-info-circle me-2" style="color: #f7ec59;"></i>Additional Information
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-md-6">
                    <small class="text-muted">Phase ID</small>
                    <div class="fw-bold font-monospace text-white">${
                      phase.phaseId
                    }</div>
                  </div>
                  <div class="col-md-6">
                    <small class="text-muted">Progress</small>
                    <div class="fw-bold text-white">${progress}%</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  function getStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-warning";
      case "in progress":
        return "bg-primary";
      case "awaiting approval":
        return "bg-info";
      case "completed":
        return "bg-success";
      case "cancelled":
        return "bg-danger";
      case "draft":
        return "badge-secondary";
      case "planning":
        return "badge-info";
      case "active":
        return "badge-primary";
      case "maintenance":
        return "badge-warning";
      default:
        return "bg-secondary";
    }
  }

  function getTaskStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-secondary";
      case "in progress":
      case "inprogress":
      case "in-progress":
        return "bg-warning";
      case "awaiting approval":
      case "awaiting-approval":
      case "awaitingapproval":
        return "bg-info";
      case "completed":
        return "bg-success";
      case "overdue":
        return "bg-danger";
      default:
        return "bg-light";
    }
  }

  function getPriorityBadgeClass(priority) {
    switch (priority.toLowerCase()) {
      case "high":
        return "bg-danger";
      case "medium":
        return "bg-warning";
      case "low":
        return "bg-success";
      default:
        return "bg-secondary";
    }
  }
});
