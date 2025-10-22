// Contractor Dashboard JavaScript

let currentFilters = {
  search: "",
  status: "",
  startDateFrom: "",
  startDateTo: "",
  dueDateFrom: "",
  dueDateTo: "",
  sortBy: "dueDate",
  sortOrder: "asc",
};

// Initialize dashboard
function initializeContractorDashboard() {
  console.log("Initializing contractor dashboard...");

  // Setup event listeners
  setupSearchHandlers();
  setupFilterHandlers();
  setupStatusFilterHandlers();

  // Load initial data
  loadDashboardData();

  console.log("Contractor dashboard initialized");
}

// Setup search functionality
function setupSearchHandlers() {
  const searchInput = document.getElementById("searchInput");
  const searchBtn = document.getElementById("searchBtn");
  const clearSearchBtn = document.getElementById("clearSearchBtn");

  if (searchInput) {
    searchInput.addEventListener("input", function () {
      currentFilters.search = this.value;
      debounceSearch();
    });

    searchInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") {
        performSearch();
      }
    });
  }

  if (searchBtn) {
    searchBtn.addEventListener("click", performSearch);
  }

  if (clearSearchBtn) {
    clearSearchBtn.addEventListener("click", function () {
      searchInput.value = "";
      currentFilters.search = "";
      performSearch();
    });
  }
}

// Setup filter handlers
function setupFilterHandlers() {
  const filterInputs = document.querySelectorAll(
    ".filter-input, .filter-select"
  );
  filterInputs.forEach((input) => {
    input.addEventListener("change", function () {
      updateCurrentFilters();
      applyFilters();
    });
  });
}

// Setup status filter handlers
function setupStatusFilterHandlers() {
  const statusButtons = document.querySelectorAll(".status-filter-btn");
  statusButtons.forEach((button) => {
    button.addEventListener("click", function (e) {
      e.preventDefault();

      // Remove active class from all buttons
      statusButtons.forEach((btn) => btn.classList.remove("active"));

      // Add active class to clicked button
      this.classList.add("active");

      // Update filter
      currentFilters.status = this.getAttribute("data-status");
      if (currentFilters.status === "All") {
        currentFilters.status = "";
      }

      applyFilters();
    });
  });
}

// Debounced search
let searchTimeout;
function debounceSearch() {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(performSearch, 300);
}

// Perform search
async function performSearch() {
  try {
    console.log("Performing search with filters:", currentFilters);

    const params = new URLSearchParams();
    if (currentFilters.search) params.set("search", currentFilters.search);
    if (currentFilters.status) params.set("status", currentFilters.status);
    if (currentFilters.startDateFrom)
      params.set("startDateFrom", currentFilters.startDateFrom);
    if (currentFilters.startDateTo)
      params.set("startDateTo", currentFilters.startDateTo);
    if (currentFilters.dueDateFrom)
      params.set("dueDateFrom", currentFilters.dueDateFrom);
    if (currentFilters.dueDateTo)
      params.set("dueDateTo", currentFilters.dueDateTo);
    if (currentFilters.sortBy) params.set("sortBy", currentFilters.sortBy);
    if (currentFilters.sortOrder)
      params.set("sortOrder", currentFilters.sortOrder);

    const response = await fetch(
      `/Contractor/GetAssignedTasks?${params.toString()}`
    );
    if (!response.ok) throw new Error("Search failed");

    const tasks = await response.json();
    renderTasks(tasks);
  } catch (error) {
    console.error("Search error:", error);
    showToast("Search failed. Please try again.", "error");
  }
}

// Apply filters
async function applyFilters() {
  await performSearch();
}

// Update current filters from form inputs
function updateCurrentFilters() {
  currentFilters.startDateFrom =
    document.getElementById("startDateFrom")?.value || "";
  currentFilters.startDateTo =
    document.getElementById("startDateTo")?.value || "";
  currentFilters.dueDateFrom =
    document.getElementById("dueDateFrom")?.value || "";
  currentFilters.dueDateTo = document.getElementById("dueDateTo")?.value || "";
  currentFilters.sortBy = document.getElementById("sortBy")?.value || "dueDate";
  currentFilters.sortOrder =
    document.getElementById("sortOrder")?.value || "asc";
}

// Load dashboard data
async function loadDashboardData() {
  try {
    console.log("Loading dashboard data...");

    // The dashboard data is already loaded server-side
    // This function can be used for additional client-side data loading
  } catch (error) {
    console.error("Error loading dashboard data:", error);
    showToast("Failed to load dashboard data", "error");
  }
}

// Render tasks
function renderTasks(tasks) {
  const tasksGrid = document.getElementById("tasksGrid");
  if (!tasksGrid) return;

  if (tasks.length === 0) {
    tasksGrid.innerHTML = `
            <div class="col-12">
                <div class="no-tasks-message">
                    <div class="text-center py-5">
                        <i class="fa-solid fa-clipboard-list fa-3x text-muted mb-3"></i>
                        <h4 class="text-muted">No tasks found</h4>
                        <p class="text-muted">No tasks match your current filters.</p>
                    </div>
                </div>
            </div>
        `;
    return;
  }

  // Clear existing tasks
  tasksGrid.innerHTML = "";

  // Render each task
  tasks.forEach((task) => {
    const taskElement = createTaskCard(task);
    tasksGrid.appendChild(taskElement);
  });
}

// Create task card element
function createTaskCard(task) {
  const card = document.createElement("div");
  card.className = "task-card card h-100 construction-border-thin";
  card.id = `task-${task.taskId}`;
  card.setAttribute("data-task-id", task.taskId);
  card.setAttribute("data-task-name", task.name);
  card.setAttribute("data-task-status", task.status);

  // Calculate derived values
  const isOverdue =
    task.dueDate < new Date().toISOString() && task.status !== "Completed";
  const daysUntilDue = Math.max(
    0,
    Math.ceil((new Date(task.dueDate) - new Date()) / (1000 * 60 * 60 * 24))
  );
  const statusBadgeClass = getStatusBadgeClass(task.status);
  const canSubmitProgress =
    task.status === "In Progress" || task.status === "InProgress";
  const canRequestCompletion =
    task.status === "In Progress" || task.status === "InProgress";

  card.innerHTML = `
        <div class="card-header">
            <div class="d-flex justify-content-between align-items-start">
                <div class="flex-grow-1">
                    <h6 class="card-title mb-1 fw-bold">${escapeHtml(
                      task.name
                    )}</h6>
                    <p class="card-subtitle small mb-0">${escapeHtml(
                      task.projectName || "Unknown Project"
                    )}</p>
                </div>
                <div class="text-end d-flex align-items-center gap-2">
                    <button type="button" class="btn btn-sm btn-outline-secondary" title="View details"
                        onclick="openTaskDetails('${task.taskId}')">
                        <i class="fa-solid fa-chevron-down"></i>
                    </button>
                    <span class="status-badge ${statusBadgeClass}">${
    task.status
  }</span>
                </div>
            </div>
        </div>
        <div class="card-body">
            ${
              task.description
                ? `
                <p class="card-text small mb-3">
                    ${escapeHtml(
                      task.description.length > 100
                        ? task.description.substring(0, 100) + "..."
                        : task.description
                    )}
                </p>
            `
                : ""
            }
            <div class="row g-2 mb-3">
                <div class="col-12">
                    <div class="stat-item mb-2">
                        <small>Start Date: <span>${new Date(
                          task.startDate
                        ).toLocaleDateString()}</span></small>
                    </div>
                </div>
                <div class="col-12">
                    <div class="stat-item mb-2">
                        <small>Due Date: <span class="${
                          isOverdue ? "text-danger fw-bold" : ""
                        }">${new Date(
    task.dueDate
  ).toLocaleDateString()}</span></small>
                    </div>
                </div>
                <div class="col-12">
                    <div class="stat-item mb-2">
                        <small>Project Budget: <span>R ${(
                          task.projectBudget || 0
                        ).toLocaleString()}</span></small>
                    </div>
                </div>
            </div>
            <div class="mb-3">
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <small class="progress-label">Task Progress:</small>
                    <small class="progress-percentage">${
                      task.progress || 0
                    }%</small>
                </div>
                <div class="progress">
                    <div class="progress-bar" role="progressbar" style="width: ${
                      task.progress || 0
                    }%;" 
                         aria-valuenow="${
                           task.progress || 0
                         }" aria-valuemin="0" aria-valuemax="100"></div>
                </div>
            </div>
            <div class="task-meta mb-3">
                <div class="d-flex justify-content-between align-items-center">
                    <span class="priority-badge priority-${(
                      task.priority || "medium"
                    ).toLowerCase()}">
                        <i class="fa-solid fa-flag me-1"></i>${
                          task.priority || "Medium"
                        }
                    </span>
                    ${
                      isOverdue
                        ? `
                        <span class="overdue-badge">
                            <i class="fa-solid fa-exclamation-triangle me-1"></i>Overdue
                        </span>
                    `
                        : daysUntilDue <= 3 && daysUntilDue > 0
                        ? `
                        <span class="due-soon-badge">
                            <i class="fa-solid fa-clock me-1"></i>Due in ${daysUntilDue} day(s)
                        </span>
                    `
                        : ""
                    }
                </div>
            </div>
        </div>
        <div class="card-footer">
            <div class="action-buttons">
                ${
                  canSubmitProgress
                    ? `
                    <button type="button" class="btn-action btn-primary" onclick="openProgressReportModal('${task.taskId}')"
                        title="Submit progress report">
                        <i class="fa-solid fa-file-lines me-1"></i>Submit Progress
                    </button>
                `
                    : task.status === "Awaiting Approval"
                    ? `
                    <button type="button" class="btn-action btn-info" disabled title="Awaiting PM approval">
                        <i class="fa-solid fa-hourglass-half me-1"></i>Awaiting Approval
                    </button>
                `
                    : task.status === "Completed"
                    ? `
                    <button type="button" class="btn-action btn-success" disabled title="Task completed">
                        <i class="fa-solid fa-check-circle me-1"></i>Completed
                    </button>
                `
                    : canRequestCompletion
                    ? `
                    <button type="button" class="btn-action btn-primary" onclick="openCompletionRequestModal('${task.taskId}')"
                        title="Request task completion">
                        <i class="fa-solid fa-check me-1"></i>Request Completion
                    </button>
                `
                    : `
                    <button type="button" class="btn-action btn-secondary" disabled title="Task not started">
                        <i class="fa-solid fa-clock me-1"></i>Not Started
                    </button>
                `
                }
                <div class="dropdown">
                    <button class="btn-action btn-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown">
                        <i class="fa-solid fa-ellipsis-v"></i>
                    </button>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" href="#" onclick="openTaskDetails('${
                          task.taskId
                        }')">
                            <i class="fa-solid fa-eye me-2"></i>View Details
                        </a></li>
                        ${
                          canSubmitProgress
                            ? `
                            <li><a class="dropdown-item" href="#" onclick="openProgressReportModal('${task.taskId}')">
                                <i class="fa-solid fa-file-lines me-2"></i>Submit Progress
                            </a></li>
                        `
                            : ""
                        }
                        ${
                          canRequestCompletion
                            ? `
                            <li><a class="dropdown-item" href="#" onclick="openCompletionRequestModal('${task.taskId}')">
                                <i class="fa-solid fa-check me-2"></i>Request Completion
                            </a></li>
                        `
                            : ""
                        }
                        <li><a class="dropdown-item" href="#" onclick="viewTaskBudget('${
                          task.taskId
                        }')">
                            <i class="fa-solid fa-dollar-sign me-2"></i>View Budget
                        </a></li>
                    </ul>
                </div>
            </div>
        </div>
    `;

  return card;
}

// Get status badge class
function getStatusBadgeClass(status) {
  switch (status.toLowerCase()) {
    case "pending":
      return "badge-secondary";
    case "in progress":
    case "inprogress":
      return "badge-warning";
    case "awaiting approval":
      return "badge-info";
    case "completed":
      return "badge-success";
    case "overdue":
      return "badge-danger";
    default:
      return "badge-light";
  }
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

// Global functions for task actions
function openTaskDetails(taskId) {
  // This will be handled by the task card partial
  console.log("Opening task details for:", taskId);
}

function openProgressReportModal(taskId) {
  // This will be handled by the progress report modal
  console.log("Opening progress report modal for:", taskId);
}

function openCompletionRequestModal(taskId) {
  // This will be handled by the completion request modal
  console.log("Opening completion request modal for:", taskId);
}

function viewTaskBudget(taskId) {
  fetch(`/Contractor/GetTaskProjectBudget?taskId=${encodeURIComponent(taskId)}`)
    .then((response) => response.json())
    .then((budget) => {
      if (budget) {
        showToast(
          `Project: ${budget.projectName}<br>Budget: R ${
            budget.budgetPlanned?.toLocaleString() || "0"
          }`,
          "info"
        );
      }
    })
    .catch((error) => {
      console.error("Failed to load budget:", error);
      showToast("Failed to load budget information", "error");
    });
}
