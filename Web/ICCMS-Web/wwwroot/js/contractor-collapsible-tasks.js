// contractor-collapsible-tasks.js - Collapsible Task Cards Functionality

document.addEventListener("DOMContentLoaded", function () {
  initializeCollapsibleTasks();
});

function initializeCollapsibleTasks() {
  // Add click handlers to all task card headers
  document.querySelectorAll(".task-card-header").forEach((header) => {
    header.addEventListener("click", function (e) {
      // Don't trigger if clicking on action buttons
      if (e.target.closest(".btn") || e.target.closest(".task-actions")) {
        return;
      }

      const taskId = this.closest(".task-card-collapsible").dataset.taskId;
      toggleTaskCard(taskId);
    });
  });

  // Add sort functionality
  const sortSelect = document.getElementById("taskSortBy");
  if (sortSelect) {
    sortSelect.addEventListener("change", function () {
      sortTasks(this.value);
    });
  }

  // Add refresh functionality
  const refreshBtn = document.getElementById("refreshTasksBtn");
  if (refreshBtn) {
    refreshBtn.addEventListener("click", function () {
      refreshTasks();
    });
  }
}

function toggleTaskCard(taskId) {
  const taskCard = document.querySelector(`[data-task-id="${taskId}"]`);
  const content = document.getElementById(`task-content-${taskId}`);
  const collapseIcon = taskCard.querySelector(".task-collapse-icon i");

  if (!taskCard || !content) return;

  const isExpanded = content.style.display !== "none";

  if (isExpanded) {
    // Collapse
    content.style.display = "none";
    taskCard.classList.remove("expanded");
    collapseIcon.classList.remove("fa-chevron-up");
    collapseIcon.classList.add("fa-chevron-down");
  } else {
    // Expand
    content.style.display = "block";
    taskCard.classList.add("expanded");
    collapseIcon.classList.remove("fa-chevron-down");
    collapseIcon.classList.add("fa-chevron-up");

    // Load task details if not already loaded
    loadTaskDetails(taskId);
  }
}

function loadTaskDetails(taskId) {
  // This function can be used to load additional task details via AJAX
  // For now, the details are already rendered in the partial view
  console.log(`Loading details for task ${taskId}`);
}

function sortTasks(sortBy) {
  const tasksContainer = document.getElementById("tasksContainer");
  if (!tasksContainer) return;

  const taskCards = Array.from(
    tasksContainer.querySelectorAll(".task-card-collapsible")
  );

  taskCards.sort((a, b) => {
    const taskIdA = a.dataset.taskId;
    const taskIdB = b.dataset.taskId;

    // Get task data from the DOM
    const taskA = getTaskDataFromDOM(a);
    const taskB = getTaskDataFromDOM(b);

    switch (sortBy) {
      case "dueDate":
        return new Date(taskA.dueDate) - new Date(taskB.dueDate);
      case "dueDateDesc":
        return new Date(taskB.dueDate) - new Date(taskA.dueDate);
      case "status":
        return taskA.status.localeCompare(taskB.status);
      case "priority":
        const priorityOrder = { High: 3, Medium: 2, Low: 1 };
        return (
          (priorityOrder[taskB.priority] || 0) -
          (priorityOrder[taskA.priority] || 0)
        );
      case "project":
        return taskA.projectName.localeCompare(taskB.projectName);
      default:
        return 0;
    }
  });

  // Re-append sorted tasks
  taskCards.forEach((card) => {
    tasksContainer.appendChild(card);
  });
}

function getTaskDataFromDOM(taskCard) {
  const header = taskCard.querySelector(".task-card-header");
  const nameElement = header.querySelector(".task-name");
  const projectElement = header.querySelector(".task-project");
  const statusElement = header.querySelector(".task-status-badge");
  const priorityElement = header.querySelector(".task-priority-badge");

  // Extract dates from the task card (you might need to adjust this based on your data structure)
  const dateTimeElement = header.querySelector(".task-date-time span");
  const dateText = dateTimeElement ? dateTimeElement.textContent : "";
  const dates = dateText.split(" - ");

  return {
    name: nameElement ? nameElement.textContent : "",
    projectName: projectElement
      ? projectElement.textContent.replace("• ", "")
      : "",
    status: statusElement ? statusElement.textContent : "",
    priority: priorityElement ? priorityElement.textContent : "",
    dueDate: dates[1]
      ? new Date(dates[1].split("/").reverse().join("-"))
      : new Date(),
    startDate: dates[0]
      ? new Date(dates[0].split("/").reverse().join("-"))
      : new Date(),
  };
}

async function refreshTasks() {
  const refreshBtn = document.getElementById("refreshTasksBtn");
  const originalText = refreshBtn.innerHTML;

  try {
    // Show loading state
    refreshBtn.innerHTML =
      '<i class="fa-solid fa-spinner fa-spin"></i> Refreshing...';
    refreshBtn.disabled = true;

    // Make API call to refresh tasks
    const response = await fetch("/Contractor/GetAssignedTasks");
    if (!response.ok) {
      throw new Error("Failed to refresh tasks");
    }

    const tasks = await response.json();

    // Update the tasks container with new data
    // This would need to be implemented with server-side rendering
    // For now, just reload the page
    window.location.reload();
  } catch (error) {
    console.error("Error refreshing tasks:", error);
    showToast("Failed to refresh tasks. Please try again.", "error");
  } finally {
    // Restore button state
    refreshBtn.innerHTML = originalText;
    refreshBtn.disabled = false;
  }
}

// Toast functionality removed - using console logging instead
function showToast(message, type = "info") {
  console.log(`📢 ${type.toUpperCase()}: ${message}`);
}

// Export functions for global access
window.toggleTaskCard = toggleTaskCard;
window.sortTasks = sortTasks;
window.refreshTasks = refreshTasks;
