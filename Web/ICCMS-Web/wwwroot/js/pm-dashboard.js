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
  const q = searchInput ? searchInput.value.trim() : "";

  const params = new URLSearchParams();
  if (q) params.set("q", q);

  fetch(`/ProjectManager/SearchProjects?${params.toString()}`, {
    credentials: "same-origin",
  })
    .then((res) => {
      if (!res.ok) throw new Error("Search failed");
      return res.json();
    })
    .then((data) => {
      hideDraftsSection();
      replaceGridHtml("projectsGrid", data.projectsHtml);
      try {
        showToast(`Results updated`, "success");
      } catch {}
    })
    .catch((err) => {
      console.error(err);
      try {
        showToast("Failed to search projects", "danger");
      } catch {}
    });
}

function initializeFilters() {
  // Status filter buttons (All, Draft, Active, Completed, Maintenance)
  const statusBtns = document.querySelectorAll(".status-filter-btn");
  statusBtns.forEach((btn) => {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      const status = this.getAttribute("data-status") || "";
      const params = new URLSearchParams();
      if (status && status !== "All") params.set("status", status);

      fetch(`/ProjectManager/SearchProjects?${params.toString()}`, {
        credentials: "same-origin",
      })
        .then((r) => (r.ok ? r.json() : Promise.reject("Status filter failed")))
        .then((data) => {
          // Behavior:
          // - All: show both sections with their respective results
          // - Draft: show only drafts and hide projects list
          // - Others: hide drafts and show matching projects
          const normalized = (status || "All").toLowerCase();
          if (normalized === "all") {
            showDraftsSection();
            replaceGridHtml("projectsGrid", data.projectsHtml);
            replaceGridHtml("draftProjectsGrid", data.draftsHtml);
          } else if (normalized === "draft") {
            showDraftsSection();
            replaceGridHtml("draftProjectsGrid", data.draftsHtml);
            // Clear projects grid to avoid mixing
            replaceGridHtml("projectsGrid", "");
          } else {
            hideDraftsSection();
            replaceGridHtml("projectsGrid", data.projectsHtml);
          }
        })
        .catch((e) => console.error(e));
    });
  });

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
  const clientFilter = document.getElementById("clientFilter")?.value || "";
  const params = new URLSearchParams();
  if (clientFilter) params.set("clientId", clientFilter);

  fetch(`/ProjectManager/SearchProjects?${params.toString()}`, {
    credentials: "same-origin",
  })
    .then((r) => (r.ok ? r.json() : Promise.reject("Filter failed")))
    .then((data) => {
      // Client filter: hide drafts, show only matching projects
      hideDraftsSection();
      replaceGridHtml("projectsGrid", data.projectsHtml);
    })
    .catch((e) => console.error(e));
}

// Clear advanced filters
function clearAdvancedFilters() {
  const inputs = ["clientFilter"]; // keep for future if needed
  inputs.forEach((id) => {
    const element = document.getElementById(id);
    if (element) {
      element.value = "";
    }
  });

  fetch(`/ProjectManager/SearchProjects`, { credentials: "same-origin" })
    .then((r) => (r.ok ? r.json() : Promise.reject("Clear filters failed")))
    .then((data) => {
      showDraftsSection();
      replaceGridHtml("projectsGrid", data.projectsHtml);
      replaceGridHtml("draftProjectsGrid", data.draftsHtml);
    })
    .catch((e) => console.error(e));
}

// Show all draft projects
function showAllDraftProjects() {
  window.location.href =
    window.location.pathname + "?statusFilter=Draft&page=1";
}

// Clear search
function clearSearch() {
  const input = document.getElementById("searchInput");
  if (input) input.value = "";
  fetch(`/ProjectManager/SearchProjects`, { credentials: "same-origin" })
    .then((r) => (r.ok ? r.json() : Promise.reject("Clear search failed")))
    .then((data) => {
      showDraftsSection();
      replaceGridHtml("projectsGrid", data.projectsHtml);
      replaceGridHtml("draftProjectsGrid", data.draftsHtml);
    })
    .catch((e) => console.error(e));
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

function renderProjectsGrid(containerId, projects) {
  const container = document.getElementById(containerId);
  if (!container) return;
  if (!projects || projects.length === 0) {
    container.innerHTML = `<div class="text-center text-muted py-4">No projects.</div>`;
    return;
  }
  container.innerHTML = projects.map((p) => projectCardHtml(p)).join("");
}

function projectCardHtml(p) {
  const name = escapeHtml(p.name || "");
  const desc = escapeHtml(p.description || "");
  const status = escapeHtml(p.status || "");
  const client = escapeHtml(p.clientId || "");
  const id = escapeHtml(p.projectId || "");
  return `
    <div class="project-card">
      <div class="project-card-header">
        <h5 class="project-title">${name}</h5>
        <span class="badge">${status}</span>
      </div>
      <div class="project-card-body">
        <p class="project-desc">${desc}</p>
        <div class="project-meta">Client: ${client}</div>
      </div>
      <div class="project-card-actions">
        <button class="btn btn-sm btn-primary" onclick="viewProjectDetails('${id}')">View</button>
      </div>
    </div>`;
}

function escapeHtml(s) {
  return String(s).replace(
    /[&<>"']/g,
    (c) =>
      ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        '"': "&quot;",
        "'": "&#39;",
      }[c])
  );
}

function replaceGridHtml(containerId, html) {
  const container = document.getElementById(containerId);
  if (!container) return;
  // Always keep the grid container; inject only card HTML (no wrapper)
  if (html && html.trim().length > 0) {
    container.innerHTML = html;
  } else {
    container.innerHTML = `<div class="text-center text-muted py-4">No projects.</div>`;
  }
}

function hideDraftsSection() {
  const section = document.getElementById("draftSection");
  if (section) section.style.display = "none";
}

function showDraftsSection() {
  const section = document.getElementById("draftSection");
  if (section) section.style.display = "block";
}
