/**
 * Universal Modal Management for Contractor Tasks
 * Clean, maintainable approach to handling all task modals
 */

// Global state
let currentTaskId = null;
let currentModalType = null;

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  console.log("🚀 Universal Modal System Initialized");
  initializeModalEventListeners();
});

/**
 * Initialize all modal event listeners
 */
function initializeModalEventListeners() {
  console.log("🔧 Setting up universal modal event listeners...");

  // Task Details Modal
  const taskDetailsModal = document.getElementById("taskDetailsModal");
  if (taskDetailsModal) {
    taskDetailsModal.addEventListener("show.bs.modal", function (event) {
      console.log("🎭 Task Details Modal opening...");
      const button = event.relatedTarget;
      if (button) {
        const taskId = button.getAttribute("data-task-id");
        console.log("🔍 Task ID from button:", taskId);
        if (taskId) {
          currentTaskId = taskId;
          currentModalType = "details";
          loadTaskDetails(taskId);
        }
      }
    });
  }

  // Progress Report Modal
  const progressReportModal = document.getElementById("progressReportModal");
  if (progressReportModal) {
    progressReportModal.addEventListener("show.bs.modal", function (event) {
      console.log("🎭 Progress Report Modal opening...");
      const button = event.relatedTarget;
      if (button) {
        const taskId = button.getAttribute("data-task-id");
        console.log("🔍 Task ID from button:", taskId);
        if (taskId) {
          currentTaskId = taskId;
          currentModalType = "progress";
          loadTaskDetailsForProgress(taskId);
        }
      }
    });
  }

  // Completion Request Modal
  const completionRequestModal = document.getElementById(
    "completionRequestModal"
  );
  if (completionRequestModal) {
    completionRequestModal.addEventListener("show.bs.modal", function (event) {
      console.log("🎭 Completion Request Modal opening...");
      const button = event.relatedTarget;
      if (button) {
        const taskId = button.getAttribute("data-task-id");
        console.log("🔍 Task ID from button:", taskId);
        if (taskId) {
          currentTaskId = taskId;
          currentModalType = "completion";
          loadTaskDetailsForCompletion(taskId);
        }
      }
    });
  }

  // Form submission event listeners
  const progressReportForm = document.getElementById("progressReportForm");
  if (progressReportForm) {
    progressReportForm.addEventListener(
      "submit",
      handleProgressReportSubmission
    );
    console.log("✅ Progress Report form submission listener added");
  } else {
    console.log("❌ Progress Report form not found!");
  }

  const completionRequestForm = document.getElementById(
    "completionRequestForm"
  );
  if (completionRequestForm) {
    completionRequestForm.addEventListener(
      "submit",
      handleCompletionRequestSubmission
    );
    console.log("✅ Completion Request form submission listener added");
  } else {
    console.log("❌ Completion Request form not found!");
  }

  // Start task button event listeners
  document.addEventListener("click", function (e) {
    if (e.target.closest(".start-task-btn")) {
      const button = e.target.closest(".start-task-btn");
      const taskId = button.getAttribute("data-task-id");
      if (taskId) {
        startTaskFromButton(taskId, button);
      }
    }
  });

  console.log("✅ Universal modal event listeners initialized");
}

/**
 * Load task details for the details modal
 */
function loadTaskDetails(taskId) {
  console.log("🔄 Loading task details for taskId:", taskId);

  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => {
      console.log("📡 API Response status:", response.status);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then((task) => {
      console.log("📋 Task data received:", task);
      if (task) {
        console.log("📋 Updating task details modal and loading reports");
        updateTaskDetailsModal(task);
        console.log("📊 Loading progress reports...");
        loadProgressReports(taskId);
        console.log("✅ Loading completion reports...");
        loadCompletionReports(taskId);
      }
    })
    .catch((error) => {
      console.error("❌ Failed to load task details:", error);
      showToast("Failed to load task details: " + error.message, "error");
    });
}

/**
 * Load task details for progress report modal
 */
function loadTaskDetailsForProgress(taskId) {
  console.log("🔄 Loading task details for progress modal, taskId:", taskId);

  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((task) => {
      console.log("📋 Progress modal task data received:", task);
      if (task) {
        document.getElementById("progressModalTaskName").textContent =
          task.name;
        document.getElementById("progressModalProjectName").textContent =
          task.projectName;
        document.getElementById("progressModalTaskDescription").textContent =
          task.description || "No description provided";
      }
    })
    .catch((error) => {
      console.error("❌ Failed to load task details for progress:", error);
    });
}

/**
 * Load task details for completion request modal
 */
function loadTaskDetailsForCompletion(taskId) {
  console.log("🔄 Loading task details for completion modal, taskId:", taskId);

  fetch(`/Contractor/GetTaskDetails?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((task) => {
      console.log("📋 Completion modal task data received:", task);
      if (task) {
        document.getElementById("completionModalTaskName").textContent =
          task.name;
        document.getElementById("completionModalProjectName").textContent =
          task.projectName;
        document.getElementById("completionModalTaskDescription").textContent =
          task.description || "No description provided";
      }
    })
    .catch((error) => {
      console.error("❌ Failed to load task details for completion:", error);
    });
}

/**
 * Update the task details modal with task data
 */
function updateTaskDetailsModal(task) {
  // Update basic information
  document.getElementById("modalTaskName").textContent = task.name;
  document.getElementById("modalProjectName").textContent = task.projectName;
  document.getElementById("modalTaskDescription").textContent =
    task.description || "No description provided";
  document.getElementById("modalStartDate").textContent = new Date(
    task.startDate
  ).toLocaleDateString();
  document.getElementById("modalDueDate").textContent = new Date(
    task.dueDate
  ).toLocaleDateString();

  // Update status badge
  const statusElement = document.getElementById("modalTaskStatus");
  statusElement.innerHTML = "";
  const statusSpan = document.createElement("span");
  statusSpan.className = `badge ${mapStatusToBadge(task.status)}`;
  statusSpan.textContent = task.status || "Unknown";
  statusElement.appendChild(statusSpan);

  // Update priority badge
  const priorityElement = document.getElementById("modalTaskPriority");
  priorityElement.innerHTML = "";
  const prioritySpan = document.createElement("span");
  prioritySpan.className = `badge priority-${
    task.priority?.toLowerCase() || "medium"
  }`;
  prioritySpan.textContent = task.priority || "Medium";
  priorityElement.appendChild(prioritySpan);

  // Update progress
  const progressBar = document.getElementById("modalTaskProgress");
  const progressText = document.getElementById("modalTaskProgressText");
  if (progressBar && progressText) {
    progressBar.style.width = `${task.progress || 0}%`;
    progressText.textContent = `${task.progress || 0}%`;
  }

  // Update hours
  const hoursProgress = document.getElementById("modalHoursProgress");
  const hoursText = document.getElementById("modalHoursText");
  if (hoursProgress && hoursText) {
    const hoursPercent =
      task.estimatedHours > 0
        ? (task.actualHours / task.estimatedHours) * 100
        : 0;
    hoursProgress.style.width = `${Math.min(hoursPercent, 100)}%`;
    hoursText.textContent = `${task.actualHours || 0} / ${
      task.estimatedHours || 0
    } hours`;
  }

  // Update budget
  const budgetText = document.getElementById("modalProjectBudget");
  if (budgetText) {
    budgetText.textContent = `R ${task.projectBudget?.toLocaleString() || "0"}`;
  }

  // Update assigned to
  const assignedToElement = document.getElementById("modalAssignedTo");
  if (assignedToElement) {
    assignedToElement.textContent = task.assignedTo || "Not assigned";
  }

  // Update created date
  const createdDateElement = document.getElementById("modalCreatedDate");
  if (createdDateElement) {
    createdDateElement.textContent = new Date(
      task.createdDate
    ).toLocaleDateString();
  }

  // Update action buttons visibility
  updateActionButtons(task);
}

/**
 * Load progress reports for the task
 */
function loadProgressReports(taskId) {
  const container = document.getElementById("progressReportsContainer");
  if (!container) return;

  fetch(`/Contractor/GetProgressReports?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((reports) => {
      if (reports && reports.length > 0) {
        container.innerHTML = reports
          .map(
            (report) => `
                    <div class="timeline-item mb-3 p-3 border rounded position-relative">
                        <div class="timeline-marker position-absolute top-0 start-0 translate-middle">
                            <i class="fa-solid fa-circle text-${getStatusClass(
                              report.status
                            )}"></i>
                        </div>
                        <div class="timeline-content ms-4">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <div>
                                    <h6 class="mb-1">${
                                      report.title || "Progress Report"
                                    }</h6>
                                    <small class="text-muted">
                                        ${new Date(
                                          report.submittedDate
                                        ).toLocaleDateString()} at ${new Date(
              report.submittedDate
            ).toLocaleTimeString()}
                                    </small>
                                </div>
                                <span class="badge bg-${getStatusClass(
                                  report.status
                                )}">
                                    <i class="fa-solid ${getStatusIcon(
                                      report.status
                                    )} me-1"></i>${report.status}
                                </span>
                            </div>
                            <p class="mb-2">${
                              report.description || "No description provided"
                            }</p>
                            <div class="row">
                                <div class="col-md-3">
                                    <small class="text-muted">Hours Worked:</small>
                                    <div class="fw-bold">${
                                      report.hoursWorked
                                    }</div>
                                </div>
                                <div class="col-md-3">
                                    <small class="text-muted">Progress:</small>
                                    <div class="fw-bold">${
                                      report.progressPercentage
                                    }%</div>
                                </div>
                                <div class="col-md-6">
                                    <small class="text-muted">Submitted By:</small>
                                    <div class="fw-bold">${
                                      report.submittedBy
                                    }</div>
                                </div>
                            </div>
                            ${
                              report.attachedDocumentIds &&
                              report.attachedDocumentIds.length > 0
                                ? `
                                <div class="mt-3">
                                    <small class="text-muted">Attachments:</small>
                                    <div class="row mt-2">
                                        ${report.attachedDocumentIds
                                          .map(
                                            (docId) => `
                                            <div class="col-md-3 mb-2">
                                                <div class="attachment-thumbnail" data-doc-id="${docId}">
                                                    <img src="${
                                                      docId.startsWith("http")
                                                        ? docId
                                                        : `/api/documents/${docId}/download`
                                                    }" 
                                                         class="attachment-image" 
                                                         onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';"
                                                         onclick="openImageModal('${docId}')" />
                                                    <div class="attachment-placeholder" style="display: none;">
                                                        <i class="fa-solid fa-file"></i>
                                                    </div>
                                                </div>
                                            </div>
                                        `
                                          )
                                          .join("")}
                                    </div>
                                </div>
                            `
                                : ""
                            }
                        </div>
                    </div>
                `
          )
          .join("");
      } else {
        container.innerHTML =
          '<p class="text-muted">No progress reports available.</p>';
      }
    })
    .catch((error) => {
      console.error("Error loading progress reports:", error);
      container.innerHTML =
        '<p class="text-danger">Failed to load progress reports.</p>';
    });
}

/**
 * Escape HTML to prevent XSS attacks
 */
function escapeHtml(text) {
  if (!text) return "";
  const map = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#039;",
  };
  return text.replace(/[&<>"']/g, function (m) {
    return map[m];
  });
}

/**
 * Get status class for styling
 */
function getStatusClass(status) {
  switch (status?.toLowerCase()) {
    case "approved":
      return "success";
    case "rejected":
      return "danger";
    case "submitted":
      return "warning";
    default:
      return "secondary";
  }
}

/**
 * Get status icon
 */
function getStatusIcon(status) {
  switch (status?.toLowerCase()) {
    case "approved":
      return "fa-check-circle";
    case "rejected":
      return "fa-times-circle";
    case "submitted":
      return "fa-clock";
    default:
      return "fa-question-circle";
  }
}

/**
 * Open image modal for viewing attachments
 */
function openImageModal(documentId) {
  // Create modal if it doesn't exist
  let modal = document.getElementById("imageModal");
  if (!modal) {
    modal = document.createElement("div");
    modal.id = "imageModal";
    modal.className = "modal fade";
    modal.innerHTML = `
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Image Preview</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body text-center">
            <img id="modalImage" src="" class="img-fluid" style="max-height: 70vh;" />
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
            <a id="downloadLink" href="" class="btn btn-primary" download>
              <i class="fa-solid fa-download me-1"></i>Download
            </a>
          </div>
        </div>
      </div>
    `;
    document.body.appendChild(modal);
  }

  // Set image source and download link
  const imageUrl = documentId.startsWith("http")
    ? documentId
    : `/api/documents/${documentId}/download`;
  document.getElementById("modalImage").src = imageUrl;
  document.getElementById("downloadLink").href = imageUrl;

  // Show modal
  const bsModal = new bootstrap.Modal(modal);
  bsModal.show();
}

/**
 * Load completion reports for the task
 */
function loadCompletionReports(taskId) {
  console.log("🚀 loadCompletionReports function called with taskId:", taskId);
  const container = document.getElementById("completionReportsContainer");
  if (!container) {
    console.log("❌ completionReportsContainer not found");
    return;
  }

  console.log("🔄 Loading completion reports for taskId:", taskId);

  fetch(`/Contractor/GetCompletionReports?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => {
      console.log("📡 Completion reports response status:", response.status);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then((reports) => {
      console.log("📋 Completion reports received:", reports);
      if (reports && reports.length > 0) {
        container.innerHTML = reports
          .map(
            (report) => `
                    <div class="timeline-item mb-3 p-3 border rounded position-relative">
                        <div class="timeline-marker position-absolute top-0 start-0 translate-middle">
                            <i class="fa-solid fa-circle text-${getStatusClass(
                              report.status
                            )}"></i>
                        </div>
                        <div class="timeline-content ms-4">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <div>
                                    <h6 class="mb-1">Completion Report</h6>
                                    <small class="text-muted">
                                        <i class="fa-solid fa-calendar me-1"></i>
                                        Completed: ${new Date(
                                          report.completionDate
                                        ).toLocaleDateString()}
                                    </small>
                                </div>
                                <span class="badge bg-${getStatusClass(
                                  report.status
                                )}">
                                    <i class="fa-solid ${getStatusIcon(
                                      report.status
                                    )} me-1"></i>${report.status}
                                </span>
                            </div>
                            <div class="row mb-2">
                                <div class="col-md-4">
                                    <small class="text-muted">Final Hours:</small>
                                    <div class="fw-bold">${
                                      report.finalHours
                                    } hours</div>
                                </div>
                                <div class="col-md-4">
                                    <small class="text-muted">Spent Amount:</small>
                                    <div class="fw-bold">R ${(report.spentAmount || 0).toLocaleString('en-ZA', {minimumFractionDigits: 0, maximumFractionDigits: 0})}</div>
                                </div>
                                <div class="col-md-4">
                                    <small class="text-muted">Submitted:</small>
                                    <div class="fw-bold">${new Date(
                                      report.submittedAt
                                    ).toLocaleDateString()}</div>
                                </div>
                            </div>
                            ${
                              report.attachedDocumentIds &&
                              report.attachedDocumentIds.length > 0
                                ? `
                                <div class="mt-3">
                                    <small class="text-muted">Attachments:</small>
                                    <div class="row mt-2">
                                        ${report.attachedDocumentIds
                                          .map(
                                            (docId) => `
                                            <div class="col-md-3 mb-2">
                                                <div class="attachment-thumbnail" data-doc-id="${docId}">
                                                    <img src="${
                                                      docId.startsWith("http")
                                                        ? docId
                                                        : `/api/documents/${docId}/download`
                                                    }" 
                                                         class="attachment-image" 
                                                         onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';"
                                                         onclick="openImageModal('${docId}')" />
                                                    <div class="attachment-placeholder" style="display: none;">
                                                        <i class="fa-solid fa-file"></i>
                                                    </div>
                                                </div>
                                            </div>
                                        `
                                          )
                                          .join("")}
                                    </div>
                                </div>
                            `
                                : ""
                            }
                            <div class="mb-2">
                                <small class="text-muted">Completion Summary:</small>
                                <p class="mb-1">${escapeHtml(
                                  report.completionSummary ||
                                    "No summary provided"
                                )}</p>
                            </div>
                            ${
                              report.qualityCheck
                                ? `
                            <div class="mb-2">
                                <small class="text-muted">Quality Check:</small>
                                <p class="mb-1">${escapeHtml(
                                  report.qualityCheck
                                )}</p>
                            </div>
                            `
                                : ""
                            }
                            ${
                              report.reviewedBy
                                ? `
                            <div class="mt-2 pt-2 border-top">
                                <small class="text-muted">
                                    <i class="fa-solid fa-user-check me-1"></i>
                                    Reviewed by: ${report.reviewedBy}
                                </small>
                                ${
                                  report.reviewedAt
                                    ? `
                                <br><small class="text-muted">
                                    <i class="fa-solid fa-clock me-1"></i>
                                    Reviewed: ${new Date(
                                      report.reviewedAt
                                    ).toLocaleDateString()}
                                </small>
                                `
                                    : ""
                                }
                                ${
                                  report.reviewNotes
                                    ? `
                                <br><small class="text-muted">Notes: ${escapeHtml(
                                  report.reviewNotes
                                )}</small>
                                `
                                    : ""
                                }
                            </div>
                            `
                                : ""
                            }
                        </div>
                    </div>
                `
          )
          .join("");
      } else {
        container.innerHTML =
          '<div class="text-muted text-center py-3">No task completion reports submitted</div>';
      }
    })
    .catch((error) => {
      console.error("❌ Failed to load completion reports:", error);
      container.innerHTML =
        '<div class="text-danger text-center py-3">Failed to load completion reports</div>';
    });
}

/**
 * Start task from button click (handles both modal and list buttons)
 */
async function startTaskFromButton(taskId, button) {
  try {
    // Show loading state
    const originalText = button.innerHTML;
    button.innerHTML =
      '<i class="fa-solid fa-spinner fa-spin me-1"></i>Starting...';
    button.disabled = true;

    const response = await fetch("/Contractor/UpdateTaskStatus", {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        taskId: taskId,
        status: "In Progress",
      }),
    });

    if (response.ok) {
      // Show success message
      showNotification("Task started successfully!", "success");

      // If this is a modal button, close the modal and refresh the page
      if (button.id === "startTaskBtn") {
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("taskDetailsModal")
        );
        if (modal) {
          modal.hide();
        }
        setTimeout(() => location.reload(), 1000);
      } else {
        // If this is a list button, just refresh the page
        setTimeout(() => location.reload(), 1000);
      }
    } else {
      const error = await response.json();
      console.error("Task start validation failed:", error);

      // Show specific error messages based on validation failure
      let errorMessage =
        "Error starting task: " + (error.error || "Unknown error");

      if (error.currentStatus) {
        errorMessage = `Cannot start task. Current status is "${error.currentStatus}". Tasks can only be started from Pending status.`;
      } else if (error.projectStatus) {
        errorMessage = `Cannot start task. Project status is "${error.projectStatus}". Tasks can only be started when the project is Active or Maintenance.`;
      }

      showNotification(errorMessage, "error");
    }
  } catch (error) {
    console.error("Error starting task:", error);
    showNotification("Error starting task: " + error.message, "error");
  } finally {
    // Reset button state
    button.innerHTML = originalText;
    button.disabled = false;
  }
}

/**
 * Show notification to user
 */
function showNotification(message, type) {
  // Create notification element
  const notification = document.createElement("div");
  notification.className = `alert alert-${
    type === "success" ? "success" : "danger"
  } alert-dismissible fade show position-fixed`;
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
  `;

  // Add to page
  document.body.appendChild(notification);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, 5000);
}

/**
 * Update action buttons visibility based on task status
 */
function updateActionButtons(task) {
  const startBtn = document.getElementById("startTaskBtn");
  const progressBtn = document.getElementById("submitProgressBtn");
  const completeBtn = document.getElementById("requestCompletionBtn");

  if (startBtn) {
    startBtn.style.display = task.status === "Pending" ? "block" : "none";
  }
  if (progressBtn) {
    progressBtn.style.display =
      task.status === "In Progress" ? "block" : "none";
  }
  if (completeBtn) {
    completeBtn.style.display =
      task.status === "In Progress" ? "block" : "none";
  }
}

/**
 * Utility functions
 */
function getStatusClass(status) {
  const statusMap = {
    Pending: "warning",
    "In Progress": "info",
    Completed: "success",
    Overdue: "danger",
  };
  return statusMap[status] || "secondary";
}

function getStatusIcon(status) {
  const iconMap = {
    Pending: "fa-clock",
    "In Progress": "fa-play",
    Completed: "fa-check",
    Overdue: "fa-exclamation-triangle",
  };
  return iconMap[status] || "fa-question";
}

function mapStatusToBadge(status) {
  const statusMap = {
    Pending: "bg-warning",
    "In Progress": "bg-info",
    Completed: "bg-success",
    Overdue: "bg-danger",
  };
  return statusMap[status] || "bg-secondary";
}

function mapPriorityToClass(priorityStr) {
  const priorityMap = {
    low: "priority-low",
    medium: "priority-medium",
    high: "priority-high",
    critical: "priority-critical",
  };
  return priorityMap[priorityStr] || "priority-medium";
}

/**
 * Toast notification function
 */
function showToast(message, type = "info") {
  console.log(`📢 ${type.toUpperCase()}: ${message}`);
  // You can implement a proper toast notification here if needed
}

/**
 * Public API functions for backward compatibility
 */
window.openTaskDetailsModal = function (taskId) {
  currentTaskId = taskId;
  const modal = new bootstrap.Modal(
    document.getElementById("taskDetailsModal")
  );
  modal.show();
  loadTaskDetails(taskId);
};

window.openProgressReportModal = function (taskId) {
  currentTaskId = taskId;
  const modal = new bootstrap.Modal(
    document.getElementById("progressReportModal")
  );
  modal.show();
  loadTaskDetailsForProgress(taskId);
};

window.openCompletionRequestModal = function (taskId) {
  currentTaskId = taskId;
  const modal = new bootstrap.Modal(
    document.getElementById("completionRequestModal")
  );
  modal.show();
  loadTaskDetailsForCompletion(taskId);
};

/**
 * Handle progress report form submission
 */
async function handleProgressReportSubmission(event) {
  event.preventDefault();
  console.log("📝 Submitting progress report...");

  const form = event.target;

  // Show loading state
  const submitBtn = document.getElementById("submitProgressReportBtn");
  const originalText = submitBtn.innerHTML;
  submitBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Submitting...';
  submitBtn.disabled = true;

  try {
    // First, get the task details to get the project ID
    const taskResponse = await fetch(
      `/Contractor/GetTaskDetails?taskId=${encodeURIComponent(currentTaskId)}`
    );
    const task = await taskResponse.json();
    const projectId = task.projectId;

    console.log(`📋 Task project ID: ${projectId}`);

    // Upload any attached files
    const attachedFileUrls = [];
    const fileInput = form.querySelector("#progressDocuments");
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
      console.log(`📎 Uploading ${fileInput.files.length} files...`);

      for (let i = 0; i < fileInput.files.length; i++) {
        const file = fileInput.files[i];
        console.log(`📎 Uploading file: ${file.name}`);

        const uploadFormData = new FormData();
        uploadFormData.append("file", file);
        uploadFormData.append("projectId", projectId);
        uploadFormData.append(
          "description",
          `Progress report attachment: ${file.name}`
        );

        const uploadResponse = await fetch(
          "https://localhost:7136/api/documents/upload",
          {
            method: "POST",
            body: uploadFormData,
          }
        );

        if (uploadResponse.ok) {
          const document = await uploadResponse.json();
          attachedFileUrls.push(document.fileUrl);
          console.log(`✅ File uploaded successfully: ${document.fileUrl}`);
        } else {
          console.error(`❌ Failed to upload file: ${file.name}`);
        }
      }
    }

    // Collect form data
    const formData = {
      taskId: currentTaskId,
      description: form.querySelector("#progressDescription").value,
      hoursWorked: parseFloat(form.querySelector("#hoursWorked").value) || 0,
      progressPercentage:
        parseInt(form.querySelector("#progressPercentage").value) || 0,
      attachedDocumentIds: attachedFileUrls,
      reviewNotes: form.querySelector("#additionalNotes").value || "", // Using reviewNotes field
    };

    console.log("📋 Submitting progress report with data:", formData);

    const response = await fetch("/Contractor/SubmitProgressReport", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(formData),
    });

    console.log("📡 Progress report response status:", response.status);

    const data = await response.json();

    if (!response.ok) {
      const errorMessage =
        data.error || data.message || `HTTP error! status: ${response.status}`;
      throw new Error(errorMessage);
    }

    if (data.success) {
      console.log("✅ Progress report submitted successfully:", data);
      showToast("Progress report submitted successfully!", "success");
    } else {
      throw new Error(data.error || "Unknown error occurred");
    }

    // Close modal
    const modal = bootstrap.Modal.getInstance(
      document.getElementById("progressReportModal")
    );
    modal.hide();

    // Reset form
    form.reset();
  } catch (error) {
    console.error("❌ Failed to submit progress report:", error);
    showToast("Failed to submit progress report: " + error.message, "error");
  } finally {
    // Restore button state
    submitBtn.innerHTML = originalText;
    submitBtn.disabled = false;
  }
}

/**
 * Handle completion request form submission
 */
async function handleCompletionRequestSubmission(event) {
  event.preventDefault();
  console.log("✅ Submitting completion request...");

  const form = event.target;

  // Collect form data for completion report
  const completionDateValue = form.querySelector("#completionDate").value;
  const finalHoursValue =
    parseFloat(form.querySelector("#finalHours").value) || 0;
  const completionSummaryValue =
    form.querySelector("#completionNotes").value || "";

  // Validate required fields
  if (!completionDateValue) {
    showToast("Completion date is required", "error");
    return;
  }

  if (finalHoursValue <= 0) {
    showToast("Final hours must be greater than 0", "error");
    return;
  }

  if (!completionSummaryValue.trim()) {
    showToast("Completion summary is required", "error");
    return;
  }

  // Upload any attached files for completion
  const attachedFileUrls = [];
  const fileInput = form.querySelector("#completionDocuments");
  if (fileInput && fileInput.files && fileInput.files.length > 0) {
    console.log(`📎 Uploading ${fileInput.files.length} completion files...`);

    for (let i = 0; i < fileInput.files.length; i++) {
      const file = fileInput.files[i];
      console.log(`📎 Uploading completion file: ${file.name}`);

      const uploadFormData = new FormData();
      uploadFormData.append("file", file);
      uploadFormData.append("projectId", currentTaskId); // Using taskId as projectId for now
      uploadFormData.append(
        "description",
        `Completion report attachment: ${file.name}`
      );

      const uploadResponse = await fetch(
        "https://localhost:7136/api/documents/upload",
        {
          method: "POST",
          body: uploadFormData,
        }
      );

      if (uploadResponse.ok) {
        const document = await uploadResponse.json();
        attachedFileUrls.push(document.fileUrl);
        console.log(
          `✅ Completion file uploaded successfully: ${document.fileUrl}`
        );
      } else {
        console.error(`❌ Failed to upload completion file: ${file.name}`);
      }
    }
  }

  const formData = {
    taskId: currentTaskId,
    completionDate: completionDateValue,
    finalHours: finalHoursValue,
    completionSummary: completionSummaryValue,
    qualityCheck: form.querySelector("#qualityCheck").value || "",
    attachedDocumentIds: attachedFileUrls,
  };

  // Show loading state
  const submitBtn = document.getElementById("submitCompletionRequestBtn");
  const originalText = submitBtn.innerHTML;
  submitBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Submitting...';
  submitBtn.disabled = true;

  fetch("/Contractor/SubmitCompletionReport", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(formData),
  })
    .then(async (response) => {
      console.log("📡 Completion report response status:", response.status);

      // Parse response to get detailed error information
      const responseData = await response.json();

      if (!response.ok) {
        // Extract error message from response
        const errorMessage =
          responseData.error ||
          responseData.message ||
          `HTTP error! status: ${response.status}`;
        throw new Error(errorMessage);
      }

      return responseData;
    })
    .then((data) => {
      console.log("✅ Completion report submitted successfully:", data);

      if (data.success) {
        showToast("Completion report submitted successfully!", "success");

        // Close modal
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("completionRequestModal")
        );
        modal.hide();

        // Reset form
        form.reset();

        // Refresh the page or update the UI as needed
        setTimeout(() => {
          window.location.reload();
        }, 1500);
      } else {
        throw new Error(data.error || "Unknown error occurred");
      }
    })
    .catch((error) => {
      console.error("❌ Failed to submit completion report:", error);
      showToast(
        "Failed to submit completion report: " + error.message,
        "error"
      );
    })
    .finally(() => {
      // Restore button state
      submitBtn.innerHTML = originalText;
      submitBtn.disabled = false;
    });
}
