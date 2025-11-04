// Contractor Task Actions JavaScript
console.log("🚀 Contractor Task Actions JavaScript loaded!");

// Global variables
let currentTaskId = null;

// Helper function to get anti-forgery token
function getAntiForgeryToken() {
  // Try to get token from input field first
  const tokenInput = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );
  if (tokenInput) {
    return tokenInput.value;
  }

  // Try to get token from meta tag
  const tokenMeta = document.querySelector(
    'meta[name="__RequestVerificationToken"]'
  );
  if (tokenMeta) {
    return tokenMeta.getAttribute("content");
  }

  // Try alternative meta tag name
  const tokenMetaAlt = document.querySelector('meta[name="csrf-token"]');
  if (tokenMetaAlt) {
    return tokenMetaAlt.getAttribute("content");
  }

  console.warn("Anti-forgery token not found");
  return null;
}

// Upload documents helper function
async function uploadDocuments(inputId) {
  const fileInput = document.getElementById(inputId);
  if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
    return [];
  }

  const formData = new FormData();

  // Append all files to FormData
  for (let i = 0; i < fileInput.files.length; i++) {
    formData.append("files", fileInput.files[i]);
  }

  // Get anti-forgery token
  const token = getAntiForgeryToken();
  const headers = {};

  if (token) {
    headers["RequestVerificationToken"] = token;
  }

  try {
    const response = await fetch("/Contractor/UploadDocuments", {
      method: "POST",
      headers: headers,
      credentials: "same-origin",
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();

    if (data.success && data.documentIds) {
      return data.documentIds;
    } else {
      throw new Error(data.error || "Upload failed");
    }
  } catch (error) {
    console.error("Error uploading documents:", error);
    showToast("Failed to upload documents: " + error.message, "error");
    throw error;
  }
}

// Initialize task actions
function initializeTaskActions() {
  console.log("Initializing contractor task actions...");

  // Setup modal event listeners
  setupModalEventListeners();

  console.log("Contractor task actions initialized");
}

// Setup modal event listeners
function setupModalEventListeners() {
  console.log("🔧 Setting up modal event listeners...");

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
    console.log("✅ Task Details Modal found, setting up event listener");
    taskDetailsModal.addEventListener("show.bs.modal", function (event) {
      console.log("🎭 Task Details Modal opening...");
      const button = event.relatedTarget;
      if (button) {
        const taskId =
          button.getAttribute("data-task-id") ||
          button.closest("[data-task-id]")?.getAttribute("data-task-id");
        console.log("🔍 Task ID from button:", taskId);
        if (taskId) {
          currentTaskId = taskId;
          loadTaskDetailsForModal(taskId);
        }
      }
    });
  } else {
    console.log("❌ Task Details Modal not found!");
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
      console.error("❌ Failed to load task details:", error);
      console.error("❌ Error details:", error.message);
      showToast("Failed to load task details: " + error.message, "error");
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
async function submitProgressReport() {
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

  try {
    // Upload documents first
    const attachedDocumentIds = await uploadDocuments("progressDocuments");

    // Prepare progress report data
    const progressReport = {
      taskId: formData.get("taskId"),
      description: description,
      hoursWorked: hoursWorked,
      status: "Submitted",
      attachedDocumentIds: attachedDocumentIds,
    };

    // Submit progress report
    const token = getAntiForgeryToken();
    const headers = {
      "Content-Type": "application/json",
    };

    if (token) {
      headers["RequestVerificationToken"] = token;
    }

    const response = await fetch("/Contractor/SubmitProgressReport", {
      method: "POST",
      headers: headers,
      credentials: "same-origin",
      body: JSON.stringify(progressReport),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();

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
        "Failed to submit progress report: " + (data.error || "Unknown error"),
        "error"
      );
    }
  } catch (error) {
    console.error("Error submitting progress report:", error);
    showToast(
      "An error occurred while submitting the progress report",
      "error"
    );
  } finally {
    // Reset button state
    submitBtn.innerHTML = originalText;
    submitBtn.disabled = false;
  }
}

// Submit completion request
async function submitCompletionRequest() {
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

  try {
    // Upload documents first
    const documentIds = await uploadDocuments("completionDocuments");

    // Prepare completion request data
    const completionRequest = {
      taskId: formData.get("taskId"),
      notes: notes,
      completionDate: formData.get("completionDate"),
      finalHours: parseFloat(formData.get("finalHours")) || 0,
      spentAmount: parseFloat(formData.get("spentAmount")) || 0,
      qualityStandards: qualityStandards,
      safetyCompliance: safetyCompliance,
      clientSatisfaction: clientSatisfaction,
      documentId: documentIds[0] ?? null,
    };

    // Submit completion request
    const token = getAntiForgeryToken();
    const headers = {
      "Content-Type": "application/json",
    };

    if (token) {
      headers["RequestVerificationToken"] = token;
    }

    const response = await fetch("/Contractor/RequestCompletion", {
      method: "POST",
      headers: headers,
      credentials: "same-origin",
      body: JSON.stringify(completionRequest),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const data = await response.json();

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
  } catch (error) {
    console.error("Error submitting completion request:", error);
    showToast(
      "An error occurred while submitting the completion request",
      "error"
    );
  } finally {
    // Reset button state
    submitBtn.innerHTML = originalText;
    submitBtn.disabled = false;
  }
}

// Start a task (update status from Pending to In Progress)
function startTask(taskId) {
  console.log("🎯 startTask called with taskId:", taskId);
  if (!taskId) {
    showToast("Task ID is required", "error");
    return;
  }

  // Show confirmation dialog
  if (
    !confirm(
      "Are you sure you want to start this task? This will change the status from 'Pending' to 'In Progress'."
    )
  ) {
    return;
  }

  // Show loading state on the button
  const startBtn = document.querySelector(
    `[data-task-id="${taskId}"].start-task-btn`
  );
  const originalText = startBtn.innerHTML;
  startBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Starting...';
  startBtn.disabled = true;

  // First get the existing task to get all required fields
  fetch(`/Contractor/GetAssignedTasks`)
    .then((response) => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then((tasks) => {
      console.log("Received tasks:", tasks);
      console.log("Looking for taskId:", taskId);
      const task = tasks.find((t) => t.taskId === taskId);
      if (!task) {
        console.error(
          "Available task IDs:",
          tasks.map((t) => t.taskId)
        );
        throw new Error("Task not found in assigned tasks");
      }
      console.log("Found task:", task);

      // Update only the status field, keep all other fields
      const updatedTask = {
        taskId: task.taskId,
        projectId: task.projectId,
        phaseId: task.phaseId,
        name: task.name,
        description: task.description,
        assignedTo: task.assignedTo,
        priority: task.priority,
        status: "In Progress",
        startDate: task.startDate,
        dueDate: task.dueDate,
        completedDate: task.completedDate,
        progress: task.progress,
        estimatedHours: task.estimatedHours,
        actualHours: task.actualHours,
      };

      // Now update the task
      // Use the Web controller endpoint which handles authentication properly
      const token = getAntiForgeryToken();
      const headers = {
        "Content-Type": "application/json",
      };

      if (token) {
        headers["RequestVerificationToken"] = token;
      }

      return fetch(`/Contractor/UpdateTaskStatus`, {
        method: "PUT",
        headers: headers,
        credentials: "same-origin",
        body: JSON.stringify({
          taskId: taskId,
          status: "In Progress",
        }),
      });
    })
    .then((response) => {
      if (!response.ok) {
        return response.text().then((text) => {
          throw new Error(
            `HTTP error! status: ${response.status}, message: ${text}`
          );
        });
      }
      return response.json();
    })
    .then((data) => {
      showToast("Task started successfully!", "success");

      // Refresh the page to update the task list
      setTimeout(() => {
        window.location.reload();
      }, 1000);
    })
    .catch((error) => {
      console.error("Error starting task:", error);
      showToast("Failed to start task. Please try again.", "error");

      // Reset button state
      startBtn.innerHTML = originalText;
      startBtn.disabled = false;
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

// Toast functionality removed - using console logging instead
function showToast(message, type = "info") {
  console.log(`📢 ${type.toUpperCase()}: ${message}`);
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  initializeTaskActions();
  setupDocumentPreview();
});
