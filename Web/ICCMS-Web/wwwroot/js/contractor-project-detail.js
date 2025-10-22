// Contractor Project Detail Page JavaScript

document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 Contractor Project Detail Page Initialized");

  // Initialize task filtering
  initializeTaskFilters();

  // Initialize task actions
  initializeTaskActions();
});

function initializeTaskFilters() {
  const statusFilter = document.getElementById("statusFilter");
  const priorityFilter = document.getElementById("priorityFilter");
  const tasksList = document.getElementById("tasksList");

  if (!statusFilter || !priorityFilter || !tasksList) {
    console.warn("Task filter elements not found");
    return;
  }

  function filterTasks() {
    const selectedStatus = statusFilter.value;
    const selectedPriority = priorityFilter.value;
    const taskCards = tasksList.querySelectorAll(".task-card");

    taskCards.forEach((card) => {
      const taskStatus = card.getAttribute("data-status");
      const taskPriority = card.getAttribute("data-priority");

      let showCard = true;

      if (selectedStatus && taskStatus !== selectedStatus) {
        showCard = false;
      }

      if (selectedPriority && taskPriority !== selectedPriority) {
        showCard = false;
      }

      card.style.display = showCard ? "block" : "none";
    });

    console.log(
      `Filtered tasks: Status=${selectedStatus}, Priority=${selectedPriority}`
    );
  }

  statusFilter.addEventListener("change", filterTasks);
  priorityFilter.addEventListener("change", filterTasks);

  console.log("✅ Task filters initialized");
}

function initializeTaskActions() {
  // Start Task buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".start-task-btn")) {
      const button = e.target.closest(".start-task-btn");
      const taskId = button.getAttribute("data-task-id");
      if (taskId) {
        startTask(taskId);
      }
    }
  });

  // View Details buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-details-btn")) {
      const button = e.target.closest(".view-details-btn");
      const taskId = button.getAttribute("data-task-id");
      if (taskId) {
        viewTaskDetails(taskId);
      }
    }
  });

  // Submit Progress buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".submit-progress-btn")) {
      const button = e.target.closest(".submit-progress-btn");
      const taskId = button.getAttribute("data-task-id");
      if (taskId) {
        submitTaskProgress(taskId);
      }
    }
  });

  // Request Completion buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".request-completion-btn")) {
      const button = e.target.closest(".request-completion-btn");
      const taskId = button.getAttribute("data-task-id");
      if (taskId) {
        requestTaskCompletion(taskId);
      }
    }
  });

  console.log("✅ Task actions initialized");
}

function startTask(taskId) {
  console.log("🎯 startTask called with taskId:", taskId);
  if (!taskId) {
    console.error("Task ID is required");
    return;
  }

  if (
    !confirm(
      "Are you sure you want to start this task? This will change the status from 'Pending' to 'In Progress'."
    )
  ) {
    return;
  }

  const startBtn = document.querySelector(
    `[data-task-id="${taskId}"].start-task-btn`
  );
  if (!startBtn) {
    console.error("Start button not found for task:", taskId);
    return;
  }

  const originalText = startBtn.innerHTML;
  startBtn.innerHTML =
    '<i class="fa-solid fa-spinner fa-spin me-1"></i>Starting...';
  startBtn.disabled = true;

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
      console.log("Task started successfully:", data);

      // Show success message
      showSuccessMessage("Task started successfully!");

      // Reload the page after a short delay
      setTimeout(() => {
        window.location.reload();
      }, 1000);
    })
    .catch((error) => {
      console.error("Error starting task:", error);
      showErrorMessage("Failed to start task. Please try again.");

      startBtn.innerHTML = originalText;
      startBtn.disabled = false;
    });
}

function viewTaskDetails(taskId) {
  console.log("🔍 Viewing details for task:", taskId);

  // This will be handled by the existing modal system
  // The modal should already be initialized by the contractor-task-actions.js
}

function submitTaskProgress(taskId) {
  console.log("📊 Submitting progress for task:", taskId);

  // This will be handled by the existing modal system
  // The modal should already be initialized by the contractor-task-actions.js
}

function requestTaskCompletion(taskId) {
  console.log("✅ Requesting completion for task:", taskId);

  // This will be handled by the existing modal system
  // The modal should already be initialized by the contractor-task-actions.js
}

function showSuccessMessage(message) {
  // Create a simple success notification
  const notification = document.createElement("div");
  notification.className = "alert alert-success position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-check-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;

  document.body.appendChild(notification);

  // Remove after 3 seconds
  setTimeout(() => {
    notification.remove();
  }, 3000);
}

function showErrorMessage(message) {
  // Create a simple error notification
  const notification = document.createElement("div");
  notification.className = "alert alert-danger position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-exclamation-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;

  document.body.appendChild(notification);

  // Remove after 5 seconds
  setTimeout(() => {
    notification.remove();
  }, 5000);
}
