// Project Manager Dashboard JavaScript

document.addEventListener("DOMContentLoaded", function () {
  initializeDashboard();
});

function initializeDashboard() {
  // Initialize collapsible sections
  initializeCollapsibleSections();

  // Initialize search functionality
  initializeSearch();

  // Initialize filters
  initializeFilters();
}

function initializeCollapsibleSections() {
  // Handle draft projects section toggle
  const collapsibleHeaders = document.querySelectorAll(".collapsible-header");

  collapsibleHeaders.forEach((header) => {
    header.addEventListener("click", function () {
      const targetId = this.getAttribute("data-target");
      const targetElement = document.getElementById(targetId);
      const toggleIcon = this.querySelector(".toggle-icon");

      console.log(
        "Draft section clicked:",
        targetId,
        targetElement,
        toggleIcon
      );

      if (targetElement) {
        const isExpanded = targetElement.style.display !== "none";

        if (isExpanded) {
          targetElement.style.display = "none";
          if (toggleIcon) {
            toggleIcon.classList.remove("fa-chevron-up");
            toggleIcon.classList.add("fa-chevron-down");
          }
          this.setAttribute("data-expanded", "false");
        } else {
          targetElement.style.display = "block";
          if (toggleIcon) {
            toggleIcon.classList.remove("fa-chevron-down");
            toggleIcon.classList.add("fa-chevron-up");
          }
          this.setAttribute("data-expanded", "true");
        }
      }
    });
  });

  // Also handle any existing draft projects content
  const draftProjectsContent = document.getElementById("draftProjects");
  if (draftProjectsContent) {
    console.log("Draft projects content found:", draftProjectsContent);
  }
}

function initializeSearch() {
  const searchInput = document.getElementById("searchInput");
  const searchBtn = document.getElementById("searchBtn");
  const clearSearchBtn = document.getElementById("clearSearchBtn");

  if (searchBtn) {
    searchBtn.addEventListener("click", function () {
      performSearch();
    });
  }

  if (searchInput) {
    searchInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") {
        performSearch();
      }
    });
  }

  if (clearSearchBtn) {
    clearSearchBtn.addEventListener("click", function () {
      searchInput.value = "";
      performSearch();
    });
  }
}

function performSearch() {
  const searchInput = document.getElementById("searchInput");
  const searchQuery = searchInput ? searchInput.value : "";

  // Build URL with search parameters
  const url = new URL(window.location);
  url.searchParams.set("searchQuery", searchQuery);
  url.searchParams.set("page", "1");

  window.location.href = url.toString();
}

function initializeFilters() {
  // Filter toggle functionality is handled in the dashboard.cshtml
}

// Toggle filters panel with enhanced UI feedback
function toggleFilters() {
  const filtersPanel = document.getElementById("filtersPanel");
  const toggleText = document.getElementById("filterToggleText");
  const toggleIcon = document.getElementById("filterToggleIcon");

  if (filtersPanel) {
    const isVisible = filtersPanel.style.display !== "none";

    if (isVisible) {
      // Hide filters
      filtersPanel.style.display = "none";
      if (toggleText) toggleText.textContent = "Show Filters";
      if (toggleIcon) {
        toggleIcon.classList.remove("fa-chevron-up");
        toggleIcon.classList.add("fa-chevron-down");
      }
    } else {
      // Show filters
      filtersPanel.style.display = "block";
      if (toggleText) toggleText.textContent = "Hide Filters";
      if (toggleIcon) {
        toggleIcon.classList.remove("fa-chevron-down");
        toggleIcon.classList.add("fa-chevron-up");
      }
    }
  }
}

// Apply advanced filters
function applyAdvancedFilters() {
  const startDateFrom = document.getElementById("startDateFrom")?.value || "";
  const startDateTo = document.getElementById("startDateTo")?.value || "";
  const clientFilter = document.getElementById("clientFilter")?.value || "";
  const budgetMin = document.getElementById("budgetMin")?.value || "";
  const budgetMax = document.getElementById("budgetMax")?.value || "";

  const url = new URL(window.location);

  // Only add parameters if they have values
  if (startDateFrom) url.searchParams.set("startDateFrom", startDateFrom);
  if (startDateTo) url.searchParams.set("startDateTo", startDateTo);
  if (clientFilter) url.searchParams.set("clientFilter", clientFilter);
  if (budgetMin) url.searchParams.set("budgetMin", budgetMin);
  if (budgetMax) url.searchParams.set("budgetMax", budgetMax);

  url.searchParams.set("page", "1"); // Reset to first page

  window.location.href = url.toString();
}

// Clear advanced filters
function clearAdvancedFilters() {
  const inputs = [
    "startDateFrom",
    "startDateTo",
    "clientFilter",
    "budgetMin",
    "budgetMax",
  ];
  inputs.forEach((id) => {
    const element = document.getElementById(id);
    if (element) {
      element.value = "";
    }
  });

  // Navigate to URL without advanced filter parameters
  const url = new URL(window.location);
  url.searchParams.delete("startDateFrom");
  url.searchParams.delete("startDateTo");
  url.searchParams.delete("clientFilter");
  url.searchParams.delete("budgetMin");
  url.searchParams.delete("budgetMax");
  url.searchParams.set("page", "1");

  window.location.href = url.toString();
}

// Show all draft projects
function showAllDraftProjects() {
  window.location.href =
    window.location.pathname + "?statusFilter=Draft&page=1";
}

// Clear search
function clearSearch() {
  window.location.href =
    window.location.pathname + "?statusFilter=" + getCurrentStatusFilter();
}

function getCurrentStatusFilter() {
  const url = new URL(window.location);
  return url.searchParams.get("statusFilter") || "All";
}

// Project card actions
function viewProjectDetails(projectId) {
  window.location.href = `/ProjectManager/ProjectDetails/${projectId}`;
}

function requestEstimate(projectId) {
  // Open estimate request modal or navigate to estimate page
  window.location.href = `/ProjectManager/RequestEstimate/${projectId}`;
}

function editProject(projectId) {
  window.location.href = `/ProjectManager/EditProject/${projectId}`;
}

function deleteProject(projectId) {
  if (
    confirm(
      "Are you sure you want to delete this project? This action cannot be undone."
    )
  ) {
    fetch(`/ProjectManager/DeleteProject/${projectId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => {
        if (response.ok) {
          location.reload();
        } else {
          alert("Failed to delete project. Please try again.");
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        alert("An error occurred while deleting the project.");
      });
  }
}

// Toast notification function
function showToast(message, type = "info") {
  // Create toast element
  const toast = document.createElement("div");
  toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
  toast.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

  document.body.appendChild(toast);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (toast.parentNode) {
      toast.parentNode.removeChild(toast);
    }
  }, 5000);
}
