// PM Task Management JavaScript
document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 PM Task Management Initialized");

  initializeTaskActions();
  initializeTaskForm();

  console.log("✅ PM Task Management components initialized");
});

function initializeTaskActions() {
  // Edit task buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".edit-task-btn")) {
      const button = e.target.closest(".edit-task-btn");
      const taskId = button.getAttribute("data-task-id");
      editTask(taskId);
    }
  });

  // Delete task buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".delete-task-btn")) {
      const button = e.target.closest(".delete-task-btn");
      const taskId = button.getAttribute("data-task-id");
      deleteTask(taskId);
    }
  });
}

function initializeTaskForm() {
  const addTaskBtn = document.getElementById("addTaskBtn");
  if (addTaskBtn) {
    addTaskBtn.addEventListener("click", function () {
      resetTaskForm();
    });
  }
}

function editTask(taskId) {
  console.log("Edit task:", taskId);

  // Show loading state
  showInfoMessage("Loading task details...");

  // In a real implementation, you would fetch the task details
  // For now, we'll simulate loading
  setTimeout(() => {
    // Populate the form with task data
    populateTaskForm({
      TaskId: taskId,
      Name: "Sample Task Name",
      Description: "Sample task description",
      PhaseId: "phase-id-1",
      Priority: "Medium",
      Status: "In Progress",
      StartDate: "2024-01-01",
      DueDate: "2024-01-15",
      EstimatedHours: 40,
      ActualHours: 20,
      Progress: 50,
      AssignedTo: "contractor-id-1",
    });

    // Show the modal
    const taskModal = document.getElementById("taskFormModal");
    if (taskModal) {
      const modal = new bootstrap.Modal(taskModal);
      modal.show();
    }
  }, 500);
}

function deleteTask(taskId) {
  console.log("Delete task:", taskId);

  if (
    !confirm(
      "Are you sure you want to delete this task? This action cannot be undone."
    )
  ) {
    return;
  }

  // Show loading state
  const taskCard = document.querySelector(`[data-task-id="${taskId}"]`);
  const deleteBtn = taskCard?.querySelector('.delete-task-btn');
  if (deleteBtn) {
    deleteBtn.disabled = true;
    deleteBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i>';
  }

  // Call API to delete task
  fetch(`/ProjectManager/DeleteTask?id=${encodeURIComponent(taskId)}`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
      'RequestVerificationToken': (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || ''
    }
  })
    .then(response => response.json())
    .then(data => {
      if (data.success) {
        // Remove the task card from the UI
        if (taskCard) {
          taskCard.remove();
        }
        showSuccessMessage("Task deleted successfully");
        // Refresh the page to update the UI
        setTimeout(() => {
          window.location.reload();
        }, 500);
      } else {
        showErrorMessage(data.message || "Failed to delete task");
        if (deleteBtn) {
          deleteBtn.disabled = false;
          deleteBtn.innerHTML = '<i class="fa-solid fa-trash"></i>';
        }
      }
    })
    .catch(error => {
      console.error("Error deleting task:", error);
      showErrorMessage("An error occurred while deleting the task");
      if (deleteBtn) {
        deleteBtn.disabled = false;
        deleteBtn.innerHTML = '<i class="fa-solid fa-trash"></i>';
      }
    });
}

function populateTaskForm(taskData) {
  // Populate form fields
  document.getElementById("taskId").value = taskData.TaskId || "";
  document.getElementById("taskName").value = taskData.Name || "";
  document.getElementById("taskDescription").value = taskData.Description || "";
  document.getElementById("taskPhase").value = taskData.PhaseId || "";
  document.getElementById("taskPriority").value = taskData.Priority || "Medium";
  document.getElementById("taskStatus").value = taskData.Status || "Pending";
  document.getElementById("taskStartDate").value = taskData.StartDate || "";
  document.getElementById("taskDueDate").value = taskData.DueDate || "";
  document.getElementById("taskEstimatedHours").value =
    taskData.EstimatedHours || "";
  document.getElementById("taskActualHours").value = taskData.ActualHours || "";
  document.getElementById("taskAssignedTo").value = taskData.AssignedTo || "";

  // Set progress slider
  const progressSlider = document.getElementById("taskProgress");
  const progressValue = document.getElementById("progressValue");
  if (progressSlider && progressValue) {
    progressSlider.value = taskData.Progress || 0;
    progressValue.textContent = (taskData.Progress || 0) + "%";
  }

  // Update modal title and button text
  document.getElementById("taskModalTitle").textContent = "Edit Task";
  document.getElementById("saveTaskBtnText").textContent = "Update Task";
}

function resetTaskForm() {
  // Clear form fields
  document.getElementById("taskId").value = "";
  document.getElementById("taskName").value = "";
  document.getElementById("taskDescription").value = "";
  document.getElementById("taskPhase").value = "";
  document.getElementById("taskPriority").value = "Medium";
  document.getElementById("taskStatus").value = "Pending";
  document.getElementById("taskEstimatedHours").value = "";
  document.getElementById("taskActualHours").value = "";
  document.getElementById("taskAssignedTo").value = "";

  // Set default dates
  const today = new Date().toISOString().split("T")[0];
  const nextWeek = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
    .toISOString()
    .split("T")[0];
  document.getElementById("taskStartDate").value = today;
  document.getElementById("taskDueDate").value = nextWeek;

  // Reset progress slider
  const progressSlider = document.getElementById("taskProgress");
  const progressValue = document.getElementById("progressValue");
  if (progressSlider && progressValue) {
    progressSlider.value = 0;
    progressValue.textContent = "0%";
  }

  // Update modal title and button text
  document.getElementById("taskModalTitle").textContent = "Add Task";
  document.getElementById("saveTaskBtnText").textContent = "Save Task";
}

// Global functions for use by other scripts
window.editTask = editTask;
window.deleteTask = deleteTask;
window.resetTaskForm = resetTaskForm;
window.populateTaskForm = populateTaskForm;

// Utility functions
function showSuccessMessage(message) {
  if (typeof window.PMProjectDetail?.showSuccessMessage === "function") {
    window.PMProjectDetail.showSuccessMessage(message);
  } else {
    console.log("Success:", message);
  }
}

function showErrorMessage(message) {
  if (typeof window.PMProjectDetail?.showErrorMessage === "function") {
    window.PMProjectDetail.showErrorMessage(message);
  } else {
    console.error("Error:", message);
  }
}

function showInfoMessage(message) {
  if (typeof window.PMProjectDetail?.showInfoMessage === "function") {
    window.PMProjectDetail.showInfoMessage(message);
  } else {
    console.log("Info:", message);
  }
}
