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
      const inputEl = document.getElementById("searchInput");
      if (inputEl) inputEl.value = "";
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

  // Calculate derived values with proper Date object comparison
  const dueDate = new Date(task.dueDate);
  const currentDate = new Date();
  const isOverdue = dueDate < currentDate && task.status !== "Completed";
  const daysUntilDue = Math.max(
    0,
    Math.ceil((dueDate - currentDate) / (1000 * 60 * 60 * 24))
  );
  const statusBadgeClass = getStatusBadgeClass(task.status);
  const canSubmitProgress =
    task.status === "In Progress" || task.status === "InProgress";
  const canRequestCompletion =
    task.status === "In Progress" || task.status === "InProgress";

  // Clamp progress to 0-100 range
  const clampedProgress = Math.max(0, Math.min(100, task.progress || 0));

  // Get safe priority class
  const priorityClass = getPriorityClass(task.priority);

  // Build card structure using DOM methods for security
  const cardHeader = document.createElement("div");
  cardHeader.className = "card-header";

  const headerContent = document.createElement("div");
  headerContent.className = "d-flex justify-content-between align-items-start";

  const headerLeft = document.createElement("div");
  headerLeft.className = "flex-grow-1";

  const cardTitle = document.createElement("h6");
  cardTitle.className = "card-title mb-1 fw-bold";
  cardTitle.textContent = task.name;

  const cardSubtitle = document.createElement("p");
  cardSubtitle.className = "card-subtitle small mb-0";
  cardSubtitle.textContent = task.projectName || "Unknown Project";

  headerLeft.appendChild(cardTitle);
  headerLeft.appendChild(cardSubtitle);

  const headerRight = document.createElement("div");
  headerRight.className = "text-end d-flex align-items-center gap-2";

  const viewDetailsBtn = document.createElement("button");
  viewDetailsBtn.type = "button";
  viewDetailsBtn.className = "btn btn-sm btn-outline-secondary";
  viewDetailsBtn.title = "View details";
  const chevronIcon = document.createElement("i");
  chevronIcon.className = "fa-solid fa-chevron-down";
  viewDetailsBtn.appendChild(chevronIcon);
  viewDetailsBtn.addEventListener("click", () => openTaskDetails(task.taskId));

  const statusBadge = document.createElement("span");
  statusBadge.className = `status-badge ${statusBadgeClass}`;
  statusBadge.textContent = task.status;

  headerRight.appendChild(viewDetailsBtn);
  headerRight.appendChild(statusBadge);

  headerContent.appendChild(headerLeft);
  headerContent.appendChild(headerRight);
  cardHeader.appendChild(headerContent);

  // Card body
  const cardBody = document.createElement("div");
  cardBody.className = "card-body";

  if (task.description) {
    const descriptionP = document.createElement("p");
    descriptionP.className = "card-text small mb-3";
    const descriptionText =
      task.description.length > 100
        ? task.description.substring(0, 100) + "..."
        : task.description;
    descriptionP.textContent = descriptionText;
    cardBody.appendChild(descriptionP);
  }

  // Stats row
  const statsRow = document.createElement("div");
  statsRow.className = "row g-2 mb-3";

  const startDateCol = document.createElement("div");
  startDateCol.className = "col-12";
  const startDateItem = document.createElement("div");
  startDateItem.className = "stat-item mb-2";
  const startDateText = document.createElement("small");
  startDateText.textContent = "Start Date: ";
  const startDateSpan = document.createElement("span");
  startDateSpan.textContent = new Date(task.startDate).toLocaleDateString();
  startDateText.appendChild(startDateSpan);
  startDateItem.appendChild(startDateText);
  startDateCol.appendChild(startDateItem);

  const dueDateCol = document.createElement("div");
  dueDateCol.className = "col-12";
  const dueDateItem = document.createElement("div");
  dueDateItem.className = "stat-item mb-2";
  const dueDateText = document.createElement("small");
  const dueDateSpan = document.createElement("span");
  if (isOverdue) {
    dueDateSpan.className = "text-danger fw-bold";
  }
  dueDateSpan.textContent = new Date(task.dueDate).toLocaleDateString();
  dueDateText.textContent = "Due Date: ";
  dueDateText.appendChild(dueDateSpan);
  dueDateItem.appendChild(dueDateText);
  dueDateCol.appendChild(dueDateItem);

  const budgetCol = document.createElement("div");
  budgetCol.className = "col-12";
  const budgetItem = document.createElement("div");
  budgetItem.className = "stat-item mb-2";
  const budgetText = document.createElement("small");
  budgetText.textContent = "Project Budget: ";
  const budgetSpan = document.createElement("span");
  budgetSpan.textContent = `R ${(task.projectBudget || 0).toLocaleString()}`;
  budgetText.appendChild(budgetSpan);
  budgetItem.appendChild(budgetText);
  budgetCol.appendChild(budgetItem);

  statsRow.appendChild(startDateCol);
  statsRow.appendChild(dueDateCol);
  statsRow.appendChild(budgetCol);
  cardBody.appendChild(statsRow);

  // Progress section
  const progressSection = document.createElement("div");
  progressSection.className = "mb-3";

  const progressHeader = document.createElement("div");
  progressHeader.className =
    "d-flex justify-content-between align-items-center mb-1";

  const progressLabel = document.createElement("small");
  progressLabel.className = "progress-label";
  progressLabel.textContent = "Task Progress:";

  const progressPercentage = document.createElement("small");
  progressPercentage.className = "progress-percentage";
  progressPercentage.textContent = `${clampedProgress}%`;

  progressHeader.appendChild(progressLabel);
  progressHeader.appendChild(progressPercentage);

  const progressBarContainer = document.createElement("div");
  progressBarContainer.className = "progress";

  const progressBar = document.createElement("div");
  progressBar.className = "progress-bar";
  progressBar.setAttribute("role", "progressbar");
  progressBar.style.width = `${clampedProgress}%`;
  progressBar.setAttribute("aria-valuenow", clampedProgress.toString());
  progressBar.setAttribute("aria-valuemin", "0");
  progressBar.setAttribute("aria-valuemax", "100");

  progressBarContainer.appendChild(progressBar);
  progressSection.appendChild(progressHeader);
  progressSection.appendChild(progressBarContainer);
  cardBody.appendChild(progressSection);

  // Task meta
  const taskMeta = document.createElement("div");
  taskMeta.className = "task-meta mb-3";

  const metaContent = document.createElement("div");
  metaContent.className = "d-flex justify-content-between align-items-center";

  const priorityBadge = document.createElement("span");
  priorityBadge.className = `priority-badge ${priorityClass}`;
  const flagIcon = document.createElement("i");
  flagIcon.className = "fa-solid fa-flag me-1";
  priorityBadge.appendChild(flagIcon);
  priorityBadge.appendChild(document.createTextNode(task.priority || "Medium"));

  metaContent.appendChild(priorityBadge);

  if (isOverdue) {
    const overdueBadge = document.createElement("span");
    overdueBadge.className = "overdue-badge";
    const warningIcon = document.createElement("i");
    warningIcon.className = "fa-solid fa-exclamation-triangle me-1";
    overdueBadge.appendChild(warningIcon);
    overdueBadge.appendChild(document.createTextNode("Overdue"));
    metaContent.appendChild(overdueBadge);
  } else if (daysUntilDue <= 3 && daysUntilDue > 0) {
    const dueSoonBadge = document.createElement("span");
    dueSoonBadge.className = "due-soon-badge";
    const clockIcon = document.createElement("i");
    clockIcon.className = "fa-solid fa-clock me-1";
    dueSoonBadge.appendChild(clockIcon);
    dueSoonBadge.appendChild(
      document.createTextNode(`Due in ${daysUntilDue} day(s)`)
    );
    metaContent.appendChild(dueSoonBadge);
  }

  taskMeta.appendChild(metaContent);
  cardBody.appendChild(taskMeta);

  // Card footer
  const cardFooter = document.createElement("div");
  cardFooter.className = "card-footer";

  const actionButtons = document.createElement("div");
  actionButtons.className = "action-buttons";

  // Main action button
  const mainActionBtn = document.createElement("button");
  mainActionBtn.type = "button";
  mainActionBtn.className = "btn-action";

  if (canSubmitProgress) {
    mainActionBtn.className += " btn-primary";
    mainActionBtn.title = "Submit progress report";
    const fileIcon = document.createElement("i");
    fileIcon.className = "fa-solid fa-file-lines me-1";
    mainActionBtn.appendChild(fileIcon);
    mainActionBtn.appendChild(document.createTextNode("Submit Progress"));
    mainActionBtn.addEventListener("click", () =>
      openProgressReportModal(task.taskId)
    );
  } else if (task.status === "Awaiting Approval") {
    mainActionBtn.className += " btn-info";
    mainActionBtn.disabled = true;
    mainActionBtn.title = "Awaiting PM approval";
    const hourglassIcon = document.createElement("i");
    hourglassIcon.className = "fa-solid fa-hourglass-half me-1";
    mainActionBtn.appendChild(hourglassIcon);
    mainActionBtn.appendChild(document.createTextNode("Awaiting Approval"));
  } else if (task.status === "Completed") {
    mainActionBtn.className += " btn-success";
    mainActionBtn.disabled = true;
    mainActionBtn.title = "Task completed";
    const checkIcon = document.createElement("i");
    checkIcon.className = "fa-solid fa-check-circle me-1";
    mainActionBtn.appendChild(checkIcon);
    mainActionBtn.appendChild(document.createTextNode("Completed"));
  } else if (canRequestCompletion) {
    mainActionBtn.className += " btn-primary";
    mainActionBtn.title = "Request task completion";
    const checkIcon = document.createElement("i");
    checkIcon.className = "fa-solid fa-check me-1";
    mainActionBtn.appendChild(checkIcon);
    mainActionBtn.appendChild(document.createTextNode("Request Completion"));
    mainActionBtn.addEventListener("click", () =>
      openCompletionRequestModal(task.taskId)
    );
  } else {
    mainActionBtn.className += " btn-secondary";
    mainActionBtn.disabled = true;
    mainActionBtn.title = "Task not started";
    const clockIcon = document.createElement("i");
    clockIcon.className = "fa-solid fa-clock me-1";
    mainActionBtn.appendChild(clockIcon);
    mainActionBtn.appendChild(document.createTextNode("Not Started"));
  }

  actionButtons.appendChild(mainActionBtn);

  // Dropdown menu
  const dropdown = document.createElement("div");
  dropdown.className = "dropdown";

  const dropdownToggle = document.createElement("button");
  dropdownToggle.className = "btn-action btn-secondary dropdown-toggle";
  dropdownToggle.type = "button";
  dropdownToggle.setAttribute("data-bs-toggle", "dropdown");
  const ellipsisIcon = document.createElement("i");
  ellipsisIcon.className = "fa-solid fa-ellipsis-v";
  dropdownToggle.appendChild(ellipsisIcon);

  const dropdownMenu = document.createElement("ul");
  dropdownMenu.className = "dropdown-menu";

  // View Details item
  const viewDetailsItem = document.createElement("li");
  const viewDetailsLink = document.createElement("a");
  viewDetailsLink.className = "dropdown-item";
  viewDetailsLink.href = "#";
  const eyeIcon = document.createElement("i");
  eyeIcon.className = "fa-solid fa-eye me-2";
  viewDetailsLink.appendChild(eyeIcon);
  viewDetailsLink.appendChild(document.createTextNode("View Details"));
  viewDetailsLink.addEventListener("click", (e) => {
    e.preventDefault();
    openTaskDetails(task.taskId);
  });
  viewDetailsItem.appendChild(viewDetailsLink);
  dropdownMenu.appendChild(viewDetailsItem);

  // Submit Progress item (if applicable)
  if (canSubmitProgress) {
    const submitProgressItem = document.createElement("li");
    const submitProgressLink = document.createElement("a");
    submitProgressLink.className = "dropdown-item";
    submitProgressLink.href = "#";
    const fileIcon = document.createElement("i");
    fileIcon.className = "fa-solid fa-file-lines me-2";
    submitProgressLink.appendChild(fileIcon);
    submitProgressLink.appendChild(document.createTextNode("Submit Progress"));
    submitProgressLink.addEventListener("click", (e) => {
      e.preventDefault();
      openProgressReportModal(task.taskId);
    });
    submitProgressItem.appendChild(submitProgressLink);
    dropdownMenu.appendChild(submitProgressItem);
  }

  // Request Completion item (if applicable)
  if (canRequestCompletion) {
    const requestCompletionItem = document.createElement("li");
    const requestCompletionLink = document.createElement("a");
    requestCompletionLink.className = "dropdown-item";
    requestCompletionLink.href = "#";
    const checkIcon = document.createElement("i");
    checkIcon.className = "fa-solid fa-check me-2";
    requestCompletionLink.appendChild(checkIcon);
    requestCompletionLink.appendChild(
      document.createTextNode("Request Completion")
    );
    requestCompletionLink.addEventListener("click", (e) => {
      e.preventDefault();
      openCompletionRequestModal(task.taskId);
    });
    requestCompletionItem.appendChild(requestCompletionLink);
    dropdownMenu.appendChild(requestCompletionItem);
  }

  // View Budget item
  const viewBudgetItem = document.createElement("li");
  const viewBudgetLink = document.createElement("a");
  viewBudgetLink.className = "dropdown-item";
  viewBudgetLink.href = "#";
  const dollarIcon = document.createElement("i");
  dollarIcon.className = "fa-solid fa-dollar-sign me-2";
  viewBudgetLink.appendChild(dollarIcon);
  viewBudgetLink.appendChild(document.createTextNode("View Budget"));
  viewBudgetLink.addEventListener("click", (e) => {
    e.preventDefault();
    viewTaskBudget(task.taskId);
  });
  viewBudgetItem.appendChild(viewBudgetLink);
  dropdownMenu.appendChild(viewBudgetItem);

  dropdown.appendChild(dropdownToggle);
  dropdown.appendChild(dropdownMenu);
  actionButtons.appendChild(dropdown);

  cardFooter.appendChild(actionButtons);

  // Assemble the card
  card.appendChild(cardHeader);
  card.appendChild(cardBody);
  card.appendChild(cardFooter);

  return card;
}

function getStatusBadgeClass(status) {
  const s = String(status || "").toLowerCase();
  switch (s) {
    case "pending":
      return "badge-secondary";
    case "in progress":
    case "inprogress":
    case "in-progress":
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

// Safe priority class mapping to prevent CSS injection
function getPriorityClass(priority) {
  const p = String(priority || "").toLowerCase();
  switch (p) {
    case "low":
      return "priority-low";
    case "medium":
      return "priority-medium";
    case "high":
      return "priority-high";
    case "urgent":
      return "priority-urgent";
    case "critical":
      return "priority-critical";
    default:
      return "priority-medium"; // Default to medium priority
  }
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
