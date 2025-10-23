// contractor-collapsible-tasks.js - Collapsible Task Cards Functionality

function initializeCollapsibleTasks() {
  // Add click handlers to all task card headers
  document.querySelectorAll(".task-card-header").forEach((header) => {
    // A11y: make header operable
    header.setAttribute("role", "button");
    header.setAttribute("tabindex", "0");
    const container = header.closest(".task-card-collapsible");
    if (container) {
      const taskId = container.dataset.taskId;
      const content = document.getElementById(`task-content-${taskId}`);
      if (content) {
        header.setAttribute("aria-controls", content.id);
        header.setAttribute("aria-expanded", String(!content.hidden));
      }
    }

    header.addEventListener("click", function (e) {
      // Ignore clicks on interactive elements
      if (
        e.target.closest(
          "a, button, [role='button'], input, select, textarea, .btn, .task-actions"
        )
      ) {
        return;
      }
      const container = this.closest(".task-card-collapsible");
      if (!container) return;
      const taskId = container.dataset.taskId;
      toggleTaskCard(taskId);
    });
  });
}

document.addEventListener("DOMContentLoaded", function () {
  initializeCollapsibleTasks();
});

function toggleTaskCard(taskId) {
  const taskCard = document.querySelector(`[data-task-id="${taskId}"]`);
  if (!taskCard) return;
  const content = document.getElementById(`task-content-${taskId}`);
  if (!content) return;
  const collapseIcon = taskCard.querySelector(".task-collapse-icon i");
  const header = taskCard.querySelector(".task-card-header");

  const isExpanded = !content.hidden;

  if (isExpanded) {
    // Collapse
    content.hidden = true;
    taskCard.classList.remove("expanded");
    if (collapseIcon) {
      collapseIcon.classList.remove("fa-chevron-up");
      collapseIcon.classList.add("fa-chevron-down");
    }
    if (header) header.setAttribute("aria-expanded", "false");
    content.setAttribute("aria-hidden", "true");
  } else {
    // Expand
    content.hidden = false;
    taskCard.classList.add("expanded");
    if (collapseIcon) {
      collapseIcon.classList.remove("fa-chevron-down");
      collapseIcon.classList.add("fa-chevron-up");
    }
    if (header) header.setAttribute("aria-expanded", "true");
    content.setAttribute("aria-hidden", "false");
    // Load task details if not already loaded
    loadTaskDetails(taskId);
  }
}

function loadTaskDetails(taskId) {
  // This function can be used to load additional task details via AJAX
  // For now, the details are already rendered in the partial view
  console.log(`Loading details for task ${taskId}`);
}

function getTaskDataFromDOM(taskCard) {
  const container = taskCard.querySelector(".task-card-header") || taskCard;
  const nameElement = container.querySelector(".task-name");
  const statusElement = container.querySelector(".task-status");
  const priorityElement = container.querySelector(".task-priority");
  const dueDateElement = container.querySelector(".task-due-date");
  const projectElement = container.querySelector(".task-project");

  return {
    name: nameElement ? nameElement.textContent : "",
    status: statusElement ? statusElement.textContent : "",
    priority: priorityElement ? priorityElement.textContent : "",
    dueDate: dueDateElement ? dueDateElement.textContent : "",
    projectName: projectElement ? projectElement.textContent : "",
  };
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

async function refreshTasks() {
  const refreshBtn = document.getElementById("refreshTasksBtn");
  const originalText = refreshBtn ? refreshBtn.innerHTML : null;
  try {
    // Show loading state
    if (refreshBtn) {
      refreshBtn.innerHTML =
        '<i class="fa-solid fa-spinner fa-spin"></i> Refreshing...';
      refreshBtn.disabled = true;
    }
    // Timeout + credentials
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000);
    const response = await fetch("/Contractor/GetAssignedTasks", {
      method: "GET",
      headers: { Accept: "application/json" },
      credentials: "same-origin",
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    if (!response.ok) {
      throw new Error(`Failed to refresh tasks (${response.status})`);
    }
    // We reload the page to reflect updates
    await response.json().catch(() => null);
    window.location.reload();
  } catch (error) {
    console.error("Error refreshing tasks:", error);
    showToast("Failed to refresh tasks. Please try again.", "error");
  } finally {
    // Restore button state
    if (refreshBtn) {
      refreshBtn.innerHTML = originalText;
      refreshBtn.disabled = false;
    }
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
