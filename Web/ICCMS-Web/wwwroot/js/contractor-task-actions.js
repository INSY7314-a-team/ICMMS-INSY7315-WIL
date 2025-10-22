// Contractor Task Actions JavaScript

// Global variables
let currentTaskId = null;

// Initialize task actions
function initializeTaskActions() {
  console.log("Initializing contractor task actions...");

  // Setup modal event listeners
  setupModalEventListeners();

  console.log("Contractor task actions initialized");
}

// Setup modal event listeners
function setupModalEventListeners() {
  // Progress Report Modal
  const progressReportModal = document.getElementById("progressReportModal");
  if (progressReportModal) {
    progressReportModal.addEventListener("show.bs.modal", function (event) {
      const button = event.relatedTarget;
      if (button) {
        const taskId =
          button.getAttribute("data-task-id") ||
          button.closest("[data-task-id]")?.getAttribute("data-task-id");
        if (taskId) {
          currentTaskId = taskId;
          loadTaskDetailsForProgressModal(taskId);
        }
      }
    });

    progressReportModal.addEventListener("hidden.bs.modal", function () {
      resetProgressReportForm();
    });
  }

  // Completion Request Modal
  const completionRequestModal = document.getElementById(
    "completionRequestModal"
  );
  if (completionRequestModal) {
    completionRequestModal.addEventListener("show.bs.modal", function (event) {
      const button = event.relatedTarget;
      if (button) {
        const taskId =
          button.getAttribute("data-task-id") ||
          button.closest("[data-task-id]")?.getAttribute("data-task-id");
        if (taskId) {
          currentTaskId = taskId;
          loadTaskDetailsForCompletionModal(taskId);
        }
      }
    });

    completionRequestModal.addEventListener("hidden.bs.modal", function () {
      resetCompletionRequestForm();
    });
  }

  // Task Details Modal
  const taskDetailsModal = document.getElementById("taskDetailsModal");
  if (taskDetailsModal) {
    taskDetailsModal.addEventListener("show.bs.modal", function (event) {
      const button = event.relatedTarget;
      if (button) {
        const taskId =
          button.getAttribute("data-task-id") ||
          button.closest("[data-task-id]")?.getAttribute("data-task-id");
        if (taskId) {
          currentTaskId = taskId;
          loadTaskDetailsForModal(taskId);
        }
      }
    });
  }
}

// Load task details for progress modal
function loadTaskDetailsForProgressModal(taskId) {
  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((task) => {
      if (task) {
        document.getElementById("modalTaskName").textContent = task.name;
        document.getElementById("modalProjectName").textContent =
          task.projectName;
        document.getElementById("modalTaskDescription").textContent =
          task.description || "No description provided";
      }
    })
    .catch((error) => {
      console.error("Failed to load task details:", error);
    });
}

// Load task details for completion modal
function loadTaskDetailsForCompletionModal(taskId) {
  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((task) => {
      if (task) {
        document.getElementById("modalTaskName").textContent = task.name;
        document.getElementById("modalProjectName").textContent =
          task.projectName;
        document.getElementById("modalTaskDescription").textContent =
          task.description || "No description provided";
      }
    })
    .catch((error) => {
      console.error("Failed to load task details:", error);
    });
}

// Load task details for modal
function loadTaskDetailsForModal(taskId) {
  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((task) => {
      if (task) {
        // Update task information
        document.getElementById("modalTaskName").textContent = task.name;
        document.getElementById("modalProjectName").textContent =
          task.projectName;
        document.getElementById("modalTaskDescription").textContent =
          task.description || "No description provided";
        document.getElementById("modalStartDate").textContent = new Date(
          task.startDate
        ).toLocaleDateString();
        document.getElementById("modalDueDate").textContent = new Date(
          task.dueDate
        ).toLocaleDateString();
        document.getElementById(
          "modalTaskStatus"
        ).innerHTML = `<span class="badge ${task.statusBadgeClass}">${task.status}</span>`;
        document.getElementById(
          "modalTaskPriority"
        ).innerHTML = `<span class="badge priority-${task.priority.toLowerCase()}">${
          task.priority
        }</span>`;

        // Update progress
        document.getElementById(
          "modalTaskProgress"
        ).style.width = `${task.progress}%`;
        document.getElementById(
          "modalTaskProgressText"
        ).textContent = `${task.progress}%`;

        // Update hours
        const hoursPercent =
          task.estimatedHours > 0
            ? (task.actualHours / task.estimatedHours) * 100
            : 0;
        document.getElementById(
          "modalHoursProgress"
        ).style.width = `${hoursPercent}%`;
        document.getElementById(
          "modalHoursText"
        ).textContent = `${task.actualHours} / ${task.estimatedHours} hours`;

        // Update budget
        document.getElementById("modalProjectBudget").textContent = `R ${
          task.projectBudget?.toLocaleString() || "0"
        }`;

        // Update action buttons visibility
        updateActionButtons(task);

        // Load progress reports
        loadProgressReportsForModal(taskId);
      }
    })
    .catch((error) => {
      console.error("Failed to load task details:", error);
      showToast("Failed to load task details", "error");
    });
}

// Update action buttons based on task status
function updateActionButtons(task) {
  const submitProgressBtn = document.getElementById("submitProgressBtn");
  const requestCompletionBtn = document.getElementById("requestCompletionBtn");

  if (submitProgressBtn) {
    submitProgressBtn.style.display = task.canSubmitProgress
      ? "inline-block"
      : "none";
  }

  if (requestCompletionBtn) {
    requestCompletionBtn.style.display = task.canRequestCompletion
      ? "inline-block"
      : "none";
  }
}

// Load progress reports for modal
function loadProgressReportsForModal(taskId) {
  const container = document.getElementById("modalProgressReports");
  if (!container) return;

  fetch(`/Contractor/GetProgressReports?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((reports) => {
      if (reports && reports.length > 0) {
        container.innerHTML = renderProgressReportsTimeline(reports);
      } else {
        container.innerHTML =
          '<div class="text-muted text-center py-3">No progress reports submitted yet</div>';
      }
    })
    .catch((error) => {
      console.error("Failed to load progress reports:", error);
      container.innerHTML =
        '<div class="text-danger text-center py-3">Failed to load progress reports</div>';
    });
}

// Render progress reports timeline
function renderProgressReportsTimeline(reports) {
  return reports
    .map((report, index) => {
      const statusClass =
        report.status === "Approved"
          ? "success"
          : report.status === "Rejected"
          ? "danger"
          : "warning";
      const statusIcon =
        report.status === "Approved"
          ? "fa-check-circle"
          : report.status === "Rejected"
          ? "fa-times-circle"
          : "fa-clock";

      return `
            <div class="timeline-item mb-3 p-3 border rounded position-relative">
                <div class="timeline-marker position-absolute top-0 start-0 translate-middle">
                    <i class="fa-solid fa-circle text-${statusClass}"></i>
                </div>
                <div class="timeline-content ms-4">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <div>
                            <h6 class="mb-1">${escapeHtml(
                              report.description || "No description"
                            )}</h6>
                            <small class="text-muted">
                                <i class="fa-solid fa-clock me-1"></i>
                                Submitted: ${new Date(
                                  report.submittedAt
                                ).toLocaleDateString()}
                            </small>
                        </div>
                        <span class="badge bg-${statusClass}">
                            <i class="fa-solid ${statusIcon} me-1"></i>${
        report.status
      }
                        </span>
                    </div>
                    <div class="row g-2">
                        <div class="col-md-3">
                            <small class="text-muted">Hours Worked:</small>
                            <div class="fw-bold">${report.hoursWorked}</div>
                        </div>
                        <div class="col-md-3">
                            <small class="text-muted">Documents:</small>
                            <div class="fw-bold">${
                              report.attachedDocumentIds?.length || 0
                            }</div>
                        </div>
                        <div class="col-md-6">
                            <small class="text-muted">Submitted By:</small>
                            <div class="fw-bold">${report.submittedBy}</div>
                        </div>
                    </div>
                    ${
                      report.reviewNotes
                        ? `
                        <div class="mt-2 p-2 bg-light rounded">
                            <small class="text-muted">Review Notes:</small>
                            <div class="small">${escapeHtml(
                              report.reviewNotes
                            )}</div>
                        </div>
                    `
                        : ""
                    }
                    ${
                      report.reviewedAt
                        ? `
                        <div class="mt-2">
                            <small class="text-muted">
                                Reviewed: ${new Date(
                                  report.reviewedAt
                                ).toLocaleDateString()}
                            </small>
                        </div>
                    `
                        : ""
                    }
                </div>
            </div>
        `;
    })
    .join("");
}

// Submit progress report
function submitProgressReport() {
  const form = document.getElementById("progressReportForm");
  if (!form) return;

  const formData = new FormData(form);

  // Validate required fields
  const description = document
    .getElementById("progressDescription")
    .value.trim();
  const hoursWorked = parseFloat(document.getElementById("hoursWorked").value);

  if (!description) {
    showToast("Please provide a work description", "error");
    return;
  }

  if (!hoursWorked || hoursWorked <= 0) {
    showToast("Please enter valid hours worked", "error");
    return;
  }

  // Show loading state
  const submitBtn = document.getElementById("submitProgressReportBtn");
  const originalText = submitBtn.innerHTML;
  submitBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Submitting...';
  submitBtn.disabled = true;

  // Prepare progress report data
  const progressReport = {
    taskId: formData.get("taskId"),
    description: description,
    hoursWorked: hoursWorked,
    status: "Submitted",
    attachedDocumentIds: [], // Will be populated after document upload
  };

  // Submit progress report
  fetch("/Contractor/SubmitProgressReport", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(progressReport),
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        showToast("Progress report submitted successfully!", "success");

        // Close modal
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("progressReportModal")
        );
        if (modal) {
          modal.hide();
        }

        // Refresh task card
        refreshTaskCard(progressReport.taskId);
      } else {
        showToast(
          "Failed to submit progress report: " +
            (data.error || "Unknown error"),
          "error"
        );
      }
    })
    .catch((error) => {
      console.error("Error submitting progress report:", error);
      showToast(
        "An error occurred while submitting the progress report",
        "error"
      );
    })
    .finally(() => {
      // Reset button state
      submitBtn.innerHTML = originalText;
      submitBtn.disabled = false;
    });
}

// Submit completion request
function submitCompletionRequest() {
  const form = document.getElementById("completionRequestForm");
  if (!form) return;

  const formData = new FormData(form);

  // Validate required fields
  const notes = document.getElementById("completionNotes").value.trim();
  const documents = document.getElementById("completionDocuments").files;

  if (!notes) {
    showToast("Please provide a completion summary", "error");
    return;
  }

  if (documents.length === 0) {
    showToast("Please upload at least one completion document", "error");
    return;
  }

  // Validate checkboxes
  const qualityStandards = document.getElementById("qualityStandards").checked;
  const safetyCompliance = document.getElementById("safetyCompliance").checked;
  const clientSatisfaction =
    document.getElementById("clientSatisfaction").checked;

  if (!qualityStandards || !safetyCompliance || !clientSatisfaction) {
    showToast("Please confirm all quality checklist items", "error");
    return;
  }

  // Show loading state
  const submitBtn = document.getElementById("submitCompletionRequestBtn");
  const originalText = submitBtn.innerHTML;
  submitBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Submitting...';
  submitBtn.disabled = true;

  // Prepare completion request data
  const completionRequest = {
    taskId: formData.get("taskId"),
    notes: notes,
    completionDate: formData.get("completionDate"),
    finalHours: parseFloat(formData.get("finalHours")) || 0,
    qualityStandards: qualityStandards,
    safetyCompliance: safetyCompliance,
    clientSatisfaction: clientSatisfaction,
    documentId: null, // Will be populated after document upload
  };

  // Submit completion request
  fetch("/Contractor/RequestCompletion", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(completionRequest),
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        showToast(
          "Completion request submitted successfully! Your project manager will review it shortly.",
          "success"
        );

        // Close modal
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("completionRequestModal")
        );
        if (modal) {
          modal.hide();
        }

        // Refresh task card
        refreshTaskCard(completionRequest.taskId);
      } else {
        showToast(
          "Failed to submit completion request: " +
            (data.error || "Unknown error"),
          "error"
        );
      }
    })
    .catch((error) => {
      console.error("Error submitting completion request:", error);
      showToast(
        "An error occurred while submitting the completion request",
        "error"
      );
    })
    .finally(() => {
      // Reset button state
      submitBtn.innerHTML = originalText;
      submitBtn.disabled = false;
    });
}

// Refresh task card after action
function refreshTaskCard(taskId) {
  const taskCard = document.getElementById(`task-${taskId}`);
  if (taskCard) {
    // Trigger a page refresh or reload the task data
    setTimeout(() => {
      window.location.reload();
    }, 1000);
  }
}

// Reset progress report form
function resetProgressReportForm() {
  const form = document.getElementById("progressReportForm");
  if (form) {
    form.reset();
  }

  const preview = document.getElementById("documentPreview");
  if (preview) {
    preview.style.display = "none";
  }

  const list = document.getElementById("documentList");
  if (list) {
    list.innerHTML = "";
  }
}

// Reset completion request form
function resetCompletionRequestForm() {
  const form = document.getElementById("completionRequestForm");
  if (form) {
    form.reset();
  }

  const preview = document.getElementById("completionDocumentPreview");
  if (preview) {
    preview.style.display = "none";
  }

  const list = document.getElementById("completionDocumentList");
  if (list) {
    list.innerHTML = "";
  }

  // Set default completion date to today
  const completionDate = document.getElementById("completionDate");
  if (completionDate) {
    completionDate.value = new Date().toISOString().split("T")[0];
  }
}

// Document preview functionality
function setupDocumentPreview() {
  const progressDocuments = document.getElementById("progressDocuments");
  if (progressDocuments) {
    progressDocuments.addEventListener("change", function (e) {
      handleDocumentPreview(e, "documentPreview", "documentList");
    });
  }

  const completionDocuments = document.getElementById("completionDocuments");
  if (completionDocuments) {
    completionDocuments.addEventListener("change", function (e) {
      handleDocumentPreview(
        e,
        "completionDocumentPreview",
        "completionDocumentList"
      );
    });
  }
}

function handleDocumentPreview(event, previewId, listId) {
  const files = Array.from(event.target.files);
  const preview = document.getElementById(previewId);
  const list = document.getElementById(listId);

  if (files.length > 0) {
    preview.style.display = "block";
    list.innerHTML = "";

    files.forEach((file, index) => {
      const item = document.createElement("div");
      item.className =
        "list-group-item d-flex justify-content-between align-items-center";
      item.innerHTML = `
                <div>
                    <i class="fa-solid fa-file me-2"></i>
                    <span>${file.name}</span>
                    <small class="text-muted ms-2">(${(
                      file.size / 1024
                    ).toFixed(1)} KB)</small>
                </div>
                <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeDocument(${index}, '${
        event.target.id
      }')">
                    <i class="fa-solid fa-times"></i>
                </button>
            `;
      list.appendChild(item);
    });
  } else {
    preview.style.display = "none";
  }
}

function removeDocument(index, inputId) {
  const input = document.getElementById(inputId);
  const dt = new DataTransfer();
  const files = Array.from(input.files);

  files.forEach((file, i) => {
    if (i !== index) {
      dt.items.add(file);
    }
  });

  input.files = dt.files;
  input.dispatchEvent(new Event("change"));
}

// Utility functions
function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `toast align-items-center text-bg-${
    type === "error" ? "danger" : type
  } border-0`;
  toast.setAttribute("role", "alert");
  toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

  document.body.appendChild(toast);
  const bsToast = new bootstrap.Toast(toast);
  bsToast.show();

  toast.addEventListener("hidden.bs.toast", () => {
    document.body.removeChild(toast);
  });
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  initializeTaskActions();
  setupDocumentPreview();
});
