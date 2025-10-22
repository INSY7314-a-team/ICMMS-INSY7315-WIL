// contractor-project-cards.js - Project Cards Functionality

document.addEventListener("DOMContentLoaded", function () {
  initializeProjectCards();
});

function initializeProjectCards() {
  // Check if project cards exist
  const projectCards = document.querySelectorAll(".project-card-collapsible");
  console.log("Found project cards:", projectCards.length);

  // Use event delegation for dynamically loaded content
  document.addEventListener("click", function (e) {
    console.log("Click detected on:", e.target);
    // Check if the clicked element is a project card header
    if (e.target.closest(".project-card-header")) {
      console.log("Project card header clicked");
      // Don't trigger if clicking on action buttons
      if (e.target.closest(".btn") || e.target.closest(".project-actions")) {
        console.log("Action button clicked, ignoring");
        return;
      }

      const projectCard = e.target.closest(".project-card-collapsible");
      if (projectCard) {
        const projectId = projectCard.dataset.projectId;
        console.log("Project ID:", projectId);
        toggleProjectCard(projectId);
      }
    }
  });

  // Add sort functionality
  const sortSelect = document.getElementById("projectSortBy");
  if (sortSelect) {
    sortSelect.addEventListener("change", function () {
      sortProjects(this.value);
    });
  }

  // Add refresh functionality
  const refreshBtn = document.getElementById("refreshProjectsBtn");
  if (refreshBtn) {
    refreshBtn.addEventListener("click", function () {
      refreshProjects();
    });
  }

  // Add task action handlers
  addTaskActionHandlers();
}

function toggleProjectCard(projectId) {
  console.log(`Toggling project card: ${projectId}`);
  const projectCard = document.querySelector(
    `[data-project-id="${projectId}"]`
  );
  const content = document.getElementById(`project-content-${projectId}`);
  const collapseIcon = projectCard
    ? projectCard.querySelector(".project-collapse-icon i")
    : null;

  console.log("Project card:", projectCard);
  console.log("Content:", content);
  console.log("Collapse icon:", collapseIcon);

  if (!projectCard || !content) {
    console.log("Missing elements - project card or content not found");
    return;
  }

  const isExpanded = content.style.display !== "none";

  if (isExpanded) {
    // Collapse
    content.style.display = "none";
    projectCard.classList.remove("expanded");
    collapseIcon.classList.remove("fa-chevron-up");
    collapseIcon.classList.add("fa-chevron-down");
  } else {
    // Expand
    content.style.display = "block";
    projectCard.classList.add("expanded");
    collapseIcon.classList.remove("fa-chevron-down");
    collapseIcon.classList.add("fa-chevron-up");

    // Load project details if not already loaded
    loadProjectDetails(projectId);
  }
}

function loadProjectDetails(projectId) {
  // This function can be used to load additional project details via AJAX
  console.log(`Loading details for project ${projectId}`);
}

function sortProjects(sortBy) {
  const projectsContainer = document.getElementById("projectsContainer");
  if (!projectsContainer) return;

  const projectCards = Array.from(
    projectsContainer.querySelectorAll(".project-card-collapsible")
  );

  projectCards.sort((a, b) => {
    const projectA = getProjectDataFromDOM(a);
    const projectB = getProjectDataFromDOM(b);

    switch (sortBy) {
      case "name":
        return projectA.name.localeCompare(projectB.name);
      case "progress":
        return projectB.progress - projectA.progress;
      case "budget":
        return projectB.budget - projectA.budget;
      case "tasks":
        return projectB.totalTasks - projectA.totalTasks;
      case "overdue":
        return projectB.overdueTasks - projectA.overdueTasks;
      default:
        return 0;
    }
  });

  // Re-append sorted projects
  projectCards.forEach((card) => {
    projectsContainer.appendChild(card);
  });
}

function getProjectDataFromDOM(projectCard) {
  const header = projectCard.querySelector(".project-card-header");
  const nameElement = header.querySelector(".project-name");
  const budgetElement = header.querySelector(".project-budget");
  const progressElement = header.querySelector(".project-progress");
  const taskCountElement = header.querySelector(".task-count");
  const overdueElement = header.querySelector(".overdue-count");

  return {
    name: nameElement ? nameElement.textContent : "",
    budget: budgetElement
      ? parseFloat(budgetElement.textContent.replace(/[^0-9.-]/g, ""))
      : 0,
    progress: progressElement
      ? parseInt(progressElement.textContent.replace("%", ""))
      : 0,
    totalTasks: taskCountElement
      ? parseInt(taskCountElement.textContent.split(" ")[0])
      : 0,
    overdueTasks: overdueElement ? parseInt(overdueElement.textContent) : 0,
  };
}

async function refreshProjects() {
  const refreshBtn = document.getElementById("refreshProjectsBtn");
  const originalText = refreshBtn.innerHTML;

  try {
    // Show loading state
    refreshBtn.innerHTML =
      '<i class="fa-solid fa-spinner fa-spin"></i> Refreshing...';
    refreshBtn.disabled = true;

    // Make API call to refresh projects
    const response = await fetch("/Contractor/GetAssignedTasks");
    if (!response.ok) {
      throw new Error("Failed to refresh projects");
    }

    const tasks = await response.json();

    // Update the projects container with new data
    // This would need to be implemented with server-side rendering
    // For now, just reload the page
    window.location.reload();
  } catch (error) {
    console.error("Error refreshing projects:", error);
    showToast("Failed to refresh projects. Please try again.", "error");
  } finally {
    // Restore button state
    refreshBtn.innerHTML = originalText;
    refreshBtn.disabled = false;
  }
}

function addTaskActionHandlers() {
  // Handle start task
  document.addEventListener("click", function (e) {
    if (e.target.closest(".start-task-btn")) {
      const taskId = e.target.closest(".start-task-btn").dataset.taskId;
      startTask(taskId);
    }
  });

  // Handle view task details
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-task-details")) {
      const taskId = e.target.closest(".view-task-details").dataset.taskId;
      viewTaskDetails(taskId);
    }
  });

  // Handle submit progress
  document.addEventListener("click", function (e) {
    if (e.target.closest(".submit-progress")) {
      const taskId = e.target.closest(".submit-progress").dataset.taskId;
      submitTaskProgress(taskId);
    }
  });

  // Handle request completion
  document.addEventListener("click", function (e) {
    if (e.target.closest(".request-completion")) {
      const taskId = e.target.closest(".request-completion").dataset.taskId;
      requestTaskCompletion(taskId);
    }
  });

  // Handle view project details
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-project-details-btn")) {
      const projectId = e.target.closest(".view-project-details-btn").dataset
        .projectId;
      viewProjectDetails(projectId);
    }
  });

  // Handle view all tasks
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-all-tasks-btn")) {
      const projectId = e.target.closest(".view-all-tasks-btn").dataset
        .projectId;
      viewAllTasks(projectId);
    }
  });
}

// Start a task (update status from Pending to In Progress)
function startTask(taskId) {
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
    '<i class="fa-solid fa-spinner fa-spin"></i> Starting...';
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
      return fetch(`/Contractor/UpdateTaskStatus`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
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

function viewTaskDetails(taskId) {
  console.log(`Viewing details for task ${taskId}`);

  // Load task details and open modal
  if (typeof loadTaskDetailsForModal === "function") {
    loadTaskDetailsForModal(taskId);
  } else {
    console.warn("loadTaskDetailsForModal function not found");
    showToast("Task details functionality not available", "warning");
  }
}

function submitTaskProgress(taskId) {
  console.log(`Submitting progress for task ${taskId}`);

  // Set current task ID for modal
  if (typeof currentTaskId !== "undefined") {
    currentTaskId = taskId;
  }

  // Load task details for progress modal and open it
  if (typeof loadTaskDetailsForProgressModal === "function") {
    loadTaskDetailsForProgressModal(taskId);
  } else {
    console.warn("loadTaskDetailsForProgressModal function not found");
  }

  // Open progress modal
  const modalElement = document.getElementById("progressReportModal");
  if (modalElement) {
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
  } else {
    showToast("Progress report modal not found", "error");
  }
}

function requestTaskCompletion(taskId) {
  console.log(`Requesting completion for task ${taskId}`);

  // Set current task ID for modal
  if (typeof currentTaskId !== "undefined") {
    currentTaskId = taskId;
  }

  // Load task details for completion modal and open it
  if (typeof loadTaskDetailsForCompletionModal === "function") {
    loadTaskDetailsForCompletionModal(taskId);
  } else {
    console.warn("loadTaskDetailsForCompletionModal function not found");
  }

  // Open completion modal
  const modalElement = document.getElementById("completionRequestModal");
  if (modalElement) {
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
  } else {
    showToast("Completion request modal not found", "error");
  }
}

function viewProjectDetails(projectId) {
  console.log(`Viewing details for project ${projectId}`);

  // Create a modal to show project details
  const modal = createProjectDetailsModal(projectId);
  document.body.appendChild(modal);

  // Show the modal
  const bsModal = new bootstrap.Modal(modal);
  bsModal.show();

  // Load tasks when modal is shown
  modal.addEventListener("shown.bs.modal", () => {
    loadProjectTasks(projectId);
  });

  // Remove modal from DOM when hidden
  modal.addEventListener("hidden.bs.modal", () => {
    document.body.removeChild(modal);
  });
}

function viewAllTasks(projectId) {
  console.log(`Viewing all tasks for project ${projectId}`);

  // Filter tasks to show only this project's tasks
  const allProjectCards = document.querySelectorAll(
    ".project-card-collapsible"
  );

  allProjectCards.forEach((card) => {
    const cardProjectId = card.dataset.projectId;
    if (cardProjectId === projectId) {
      // Expand this project card
      const content = document.getElementById(`project-content-${projectId}`);
      if (content && content.style.display === "none") {
        toggleProjectCard(projectId);
      }
      // Scroll to this project card
      card.scrollIntoView({ behavior: "smooth", block: "center" });
    } else {
      // Collapse other project cards
      const content = document.getElementById(
        `project-content-${cardProjectId}`
      );
      if (content && content.style.display !== "none") {
        toggleProjectCard(cardProjectId);
      }
    }
  });

  showToast(`Showing all tasks for project ${projectId}`, "info");
}

function createProjectDetailsModal(projectId) {
  // Find the project card to get its data
  const projectCard = document.querySelector(
    `[data-project-id="${projectId}"]`
  );
  if (!projectCard) {
    console.error("Project card not found");
    return null;
  }

  // Extract project data from the DOM
  const projectName =
    projectCard.querySelector(".project-name")?.textContent ||
    "Unknown Project";
  const projectBudget =
    projectCard.querySelector(".project-budget")?.textContent || "N/A";
  const projectProgress =
    projectCard.querySelector(".project-progress")?.textContent || "0%";
  const totalTasks =
    projectCard.querySelector(".task-count")?.textContent || "0 tasks";
  const completedTasks =
    projectCard.querySelector(".completed-count")?.textContent || "0 completed";
  const inProgressTasks =
    projectCard.querySelector(".in-progress-count")?.textContent ||
    "0 in progress";

  // Create modal HTML
  const modal = document.createElement("div");
  modal.className = "modal fade";
  modal.id = "projectDetailsModal";
  modal.setAttribute("tabindex", "-1");
  modal.setAttribute("aria-labelledby", "projectDetailsModalLabel");
  modal.setAttribute("aria-hidden", "true");

  modal.innerHTML = `
    <div class="modal-dialog modal-lg">
      <div class="modal-content dark-theme">
        <div class="modal-header">
          <h5 class="modal-title" id="projectDetailsModalLabel">
            <i class="fa-solid fa-folder-open me-2"></i>Project Details
          </h5>
          <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
        </div>
        <div class="modal-body">
          <div class="row">
            <div class="col-md-6">
              <h6><i class="fa-solid fa-info-circle me-2"></i>Project Information</h6>
              <div class="detail-row">
                <span class="detail-label">Project Name:</span>
                <span class="detail-value">${projectName}</span>
              </div>
              <div class="detail-row">
                <span class="detail-label">Project ID:</span>
                <span class="detail-value">${projectId}</span>
              </div>
              <div class="detail-row">
                <span class="detail-label">Budget:</span>
                <span class="detail-value">${projectBudget}</span>
              </div>
              <div class="detail-row">
                <span class="detail-label">Progress:</span>
                <span class="detail-value">${projectProgress}</span>
              </div>
            </div>
            <div class="col-md-6">
              <h6><i class="fa-solid fa-chart-bar me-2"></i>Task Statistics</h6>
              <div class="detail-row">
                <span class="detail-label">Total Tasks:</span>
                <span class="detail-value">${totalTasks}</span>
              </div>
              <div class="detail-row">
                <span class="detail-label">Completed:</span>
                <span class="detail-value">${completedTasks}</span>
              </div>
              <div class="detail-row">
                <span class="detail-label">In Progress:</span>
                <span class="detail-value">${inProgressTasks}</span>
              </div>
            </div>
          </div>
          <div class="mt-4">
            <h6><i class="fa-solid fa-tasks me-2"></i>Project Tasks</h6>
            <div id="projectTasksList">
              <!-- Tasks will be loaded here -->
              <p class="text-muted">Loading tasks...</p>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
          <button type="button" class="btn btn-primary" onclick="viewAllTasks('${projectId}')" data-bs-dismiss="modal">
            <i class="fa-solid fa-list me-2"></i>View All Tasks
          </button>
        </div>
      </div>
    </div>
  `;

  return modal;
}

function loadProjectTasks(projectId) {
  console.log(`Loading tasks for project ${projectId}`);

  // Find the project card to get its tasks
  const projectCard = document.querySelector(
    `[data-project-id="${projectId}"]`
  );
  if (!projectCard) {
    console.error("Project card not found");
    return;
  }

  // Get the tasks from the project card
  const taskItems = projectCard.querySelectorAll(".task-item");
  const tasksList = document.getElementById("projectTasksList");

  if (!tasksList) {
    console.error("Tasks list container not found");
    return;
  }

  if (taskItems.length === 0) {
    tasksList.innerHTML =
      '<p class="text-muted">No tasks found for this project.</p>';
    return;
  }

  // Create HTML for tasks
  let tasksHTML = "";
  taskItems.forEach((taskItem, index) => {
    const taskName =
      taskItem.querySelector(".task-item-name")?.textContent || "Unknown Task";
    const taskDates =
      taskItem.querySelector(".task-item-dates")?.textContent || "";
    const taskStatus =
      taskItem.querySelector(".task-item-status")?.textContent || "";
    const taskPriority =
      taskItem.querySelector(".task-item-priority")?.textContent || "";
    const isOverdue = taskItem.classList.contains("overdue");

    tasksHTML += `
      <div class="task-item ${isOverdue ? "overdue" : ""}">
        <div class="task-item-header">
          <div class="task-item-left">
            <span class="task-item-name">${taskName}</span>
            <span class="task-item-dates">${taskDates}</span>
          </div>
          <div class="task-item-right">
            <span class="task-item-status ${taskStatus
              .toLowerCase()
              .replace(" ", "-")}">${taskStatus}</span>
            <span class="task-item-priority priority-${taskPriority.toLowerCase()}">${taskPriority}</span>
            ${isOverdue ? '<span class="overdue-badge">OVERDUE</span>' : ""}
          </div>
        </div>
      </div>
    `;
  });

  tasksList.innerHTML = tasksHTML;
  console.log(`Loaded ${taskItems.length} tasks for project ${projectId}`);
}

// Toast functionality removed - using console logging instead
function showToast(message, type = "info") {
  console.log(`📢 ${type.toUpperCase()}: ${message}`);
}

// Export functions for global access
window.toggleProjectCard = toggleProjectCard;
window.sortProjects = sortProjects;
window.refreshProjects = refreshProjects;
window.viewProjectDetails = viewProjectDetails;
window.viewAllTasks = viewAllTasks;
window.startTask = startTask;
window.viewTaskDetails = viewTaskDetails;
window.submitTaskProgress = submitTaskProgress;
window.requestTaskCompletion = requestTaskCompletion;

// Debug function to test project card expansion
window.testProjectCard = function () {
  const projectCards = document.querySelectorAll(".project-card-collapsible");
  console.log("Available project cards:", projectCards);
  if (projectCards.length > 0) {
    const firstProjectId = projectCards[0].dataset.projectId;
    console.log("Testing with project ID:", firstProjectId);
    toggleProjectCard(firstProjectId);
  }
};
