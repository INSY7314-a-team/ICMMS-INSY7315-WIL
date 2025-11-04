// Project Analytics Charts Initialization
document.addEventListener("DOMContentLoaded", function () {
  // Initialize all charts
  initializeTaskStatusPieChart();
  initializeBudgetByPhaseBarChart();
  initializeBudgetHistoryLineChart();
  initializeGanttChart();

  // Initialize table filters
  initializeTableFilters();

  // Handle window resize for responsive charts
  let resizeTimer;
  window.addEventListener("resize", function () {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function () {
      // Charts will auto-resize with Chart.js responsive option
    }, 250);
  });
});

// Pie Chart: Task Status Breakdown
function initializeTaskStatusPieChart() {
  const ctx = document.getElementById("taskStatusPieChart");
  if (!ctx || !analyticsData.taskStatusBreakdown) return;

  const breakdown = analyticsData.taskStatusBreakdown;
  const labels = Object.keys(breakdown);
  const data = Object.values(breakdown);

  // Filter out zero values
  const filteredData = labels
    .map((label, index) => ({
      label,
      value: data[index],
    }))
    .filter((item) => item.value > 0);

  if (filteredData.length === 0) {
    ctx.parentElement.innerHTML =
      '<div class="text-center text-muted py-4"><p>No task data available</p></div>';
    return;
  }

  new Chart(ctx, {
    type: "pie",
    data: {
      labels: filteredData.map((item) => item.label),
      datasets: [
        {
          data: filteredData.map((item) => item.value),
          backgroundColor: [
            "#6c757d", // Pending - gray
            "#ffc107", // In Progress - yellow
            "#198754", // Completed - green
            "#dc3545", // Overdue - red
          ],
          borderColor: "#2A2B35",
          borderWidth: 2,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: "bottom",
          labels: {
            color: "#fff",
            padding: 15,
            font: {
              size: 12,
            },
          },
        },
        tooltip: {
          backgroundColor: "#2A2B35",
          titleColor: "#F7EC59",
          bodyColor: "#fff",
          borderColor: "#F7EC59",
          borderWidth: 1,
          padding: 12,
        },
      },
    },
  });
}

// Bar Chart: Budget by Phase
function initializeBudgetByPhaseBarChart() {
  const ctx = document.getElementById("budgetByPhaseBarChart");
  if (!ctx || !analyticsData.budgetByPhase) return;

  const phases = analyticsData.budgetByPhase;

  if (phases.length === 0) {
    ctx.parentElement.innerHTML =
      '<div class="text-center text-muted py-4"><p>No phase budget data available</p></div>';
    return;
  }

  new Chart(ctx, {
    type: "bar",
    data: {
      labels: phases.map((p) => p.phaseName),
      datasets: [
        {
          label: "Planned Budget",
          data: phases.map((p) => p.plannedBudget),
          backgroundColor: "#0dcaf0", // Info blue
          borderColor: "#0dcaf0",
          borderWidth: 1,
        },
        {
          label: "Actual Budget",
          data: phases.map((p) => p.actualBudget),
          backgroundColor: "#F7EC59", // Yellow
          borderColor: "#F7EC59",
          borderWidth: 1,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: "bottom",
          labels: {
            color: "#fff",
            padding: 10,
            font: {
              size: 11,
            },
          },
        },
        tooltip: {
          backgroundColor: "#2A2B35",
          titleColor: "#F7EC59",
          bodyColor: "#fff",
          borderColor: "#F7EC59",
          borderWidth: 1,
          padding: 10,
          callbacks: {
            label: function (context) {
              return (
                context.dataset.label +
                ": R " +
                context.parsed.y.toLocaleString("en-ZA", {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })
              );
            },
          },
        },
      },
      scales: {
        x: {
          ticks: {
            color: "#fff",
            font: {
              size: 10,
            },
          },
          grid: {
            color: "rgba(255, 255, 255, 0.1)",
          },
        },
        y: {
          ticks: {
            color: "#fff",
            font: {
              size: 10,
            },
            callback: function (value) {
              return "R " + value.toLocaleString("en-ZA");
            },
          },
          grid: {
            color: "rgba(255, 255, 255, 0.1)",
          },
        },
      },
    },
  });
}

// Line Chart: Budget Tracking Over Time
function initializeBudgetHistoryLineChart() {
  const ctx = document.getElementById("budgetHistoryLineChart");
  if (!ctx || !analyticsData.budgetHistory) return;

  const history = analyticsData.budgetHistory;

  if (history.length === 0) {
    ctx.parentElement.innerHTML =
      '<div class="text-center text-muted py-4"><p>No budget history data available</p></div>';
    return;
  }

  // Sort by date
  history.sort((a, b) => new Date(a.date) - new Date(b.date));

  new Chart(ctx, {
    type: "line",
    data: {
      labels: history.map((h) =>
        new Date(h.date).toLocaleDateString("en-ZA", {
          month: "short",
          day: "numeric",
        })
      ),
      datasets: [
        {
          label: "Planned Budget",
          data: history.map((h) => h.planned),
          borderColor: "#0dcaf0", // Info blue
          backgroundColor: "rgba(13, 202, 240, 0.1)",
          borderWidth: 2,
          fill: false,
          tension: 0.4,
        },
        {
          label: "Actual Budget",
          data: history.map((h) => h.actual),
          borderColor: "#F7EC59", // Yellow
          backgroundColor: "rgba(247, 236, 89, 0.1)",
          borderWidth: 2,
          fill: false,
          tension: 0.4,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: "bottom",
          labels: {
            color: "#fff",
            padding: 10,
            font: {
              size: 11,
            },
          },
        },
        tooltip: {
          backgroundColor: "#2A2B35",
          titleColor: "#F7EC59",
          bodyColor: "#fff",
          borderColor: "#F7EC59",
          borderWidth: 1,
          padding: 10,
          callbacks: {
            label: function (context) {
              return (
                context.dataset.label +
                ": R " +
                context.parsed.y.toLocaleString("en-ZA", {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })
              );
            },
          },
        },
      },
      scales: {
        x: {
          ticks: {
            color: "#fff",
            font: {
              size: 10,
            },
          },
          grid: {
            color: "rgba(255, 255, 255, 0.1)",
          },
        },
        y: {
          ticks: {
            color: "#fff",
            font: {
              size: 10,
            },
            callback: function (value) {
              return "R " + value.toLocaleString("en-ZA");
            },
          },
          grid: {
            color: "rgba(255, 255, 255, 0.1)",
          },
        },
      },
    },
  });
}

// Gantt Chart: Project Timeline
function initializeGanttChart() {
  const container = document.getElementById("ganttChartContainer");
  if (!container || !analyticsData.ganttData) return;

  const ganttData = analyticsData.ganttData;

  if (ganttData.length === 0) {
    container.innerHTML =
      '<div class="text-center text-muted py-4"><p>No timeline data available</p></div>';
    return;
  }

  try {
    // Convert our data format to Frappe Gantt format
    const tasks = ganttData.map((item) => {
      // Format dates for Frappe Gantt (YYYY-MM-DD)
      const startDate = new Date(item.start);
      const endDate = new Date(item.end);

      return {
        id: item.id,
        name: item.name,
        start: startDate.toISOString().split("T")[0],
        end: endDate.toISOString().split("T")[0],
        progress: item.progress,
        custom_class: item.type === "phase" ? "phase-task" : "task-item",
        dependencies: item.parentId ? [item.parentId] : [],
      };
    });

    // Initialize Frappe Gantt
    const gantt = new Gantt(container, tasks, {
      view_mode: "Month",
      language: "en",
      header_height: 60,
      column_width: 35,
      step: 24,
      bar_height: 36,
      bar_corner_radius: 5,
      arrow_curve: 5,
      padding: 20,
      date_format: "YYYY-MM-DD",
      on_click: function (task) {
        console.log("Task clicked:", task);
      },
      on_date_change: function (task, start, end) {
        console.log("Task date changed:", task, start, end);
      },
      on_progress_change: function (task, progress) {
        console.log("Task progress changed:", task, progress);
      },
      on_view_change: function (mode) {
        console.log("View mode changed:", mode);
      },
      custom_popup_html: function (task) {
        return `
                    <div class="gantt-popup">
                        <strong>${task.name}</strong><br>
                        Progress: ${task.progress}%<br>
                        Start: ${task._start.toLocaleDateString()}<br>
                        End: ${task._end.toLocaleDateString()}
                    </div>
                `;
      },
    });

    // Add custom styling for phases vs tasks - enhanced for readability
    const style = document.createElement("style");
    style.textContent = `
            .gantt .bar-wrapper .bar.phase-task {
                fill: #F7EC59;
                stroke: rgb(255, 255, 255);
                stroke-width: 2;
            }
            .gantt .bar-wrapper .bar.task-item {
                fill: #0dcaf0;
                stroke: rgba(0, 0, 0, 0.3);
                stroke-width: 2;
            }
            .gantt .grid-background {
                fill: #2A2B35;
            }
            .gantt .grid-header {
                fill: #1A1B25;
                stroke: rgba(247, 236, 89, 0.3);
                stroke-width: 1;
            }
            .gantt .today-highlight {
                fill: rgba(247, 236, 89, 0.25);
                stroke: #F7EC59;
                stroke-width: 2;
                stroke-dasharray: 4, 4;
            }
            .gantt .lower-text, .gantt .upper-text {
                fill: #ffffff !important;
                font-weight: 700 !important;
                font-size: 12px !important;
                text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8) !important;
            }
            .gantt .bar-label {
                fill: #ffffff !important;
                font-weight: 800 !important;
                font-size: 12px !important;
                text-shadow: 
                    1px 1px 3px rgba(0, 0, 0, 0.9),
                    0 0 4px rgba(0, 0, 0, 0.7) !important;
                letter-spacing: 0.4px !important;
            }
            .gantt .row-text {
                fill: #ffffff !important;
                font-weight: 700 !important;
                font-size: 13px !important;
                text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8) !important;
            }
            .gantt .row-line {
                stroke: rgba(255, 255, 255, 0.15);
                stroke-width: 1;
            }
            .gantt .bar-wrapper:hover .bar {
                filter: drop-shadow(0 4px 8px rgba(247, 236, 89, 0.4));
                stroke-width: 3;
            }
        `;
    document.head.appendChild(style);
  } catch (error) {
    console.error("Error initializing Gantt chart:", error);
    container.innerHTML =
      '<div class="text-center text-muted py-4"><p>Error loading timeline chart</p></div>';
  }
}

// Table Filters for Quotes and Invoices
function initializeTableFilters() {
  // Quote filters
  const quotesTable = document.getElementById("quotesTableBody");

  if (quotesTable) {
    const trackingCard = quotesTable.closest(".tracking-card");
    if (trackingCard) {
      const quoteFilterButtons = trackingCard.querySelectorAll(
        ".tracking-header .filter-btn"
      );
      quoteFilterButtons.forEach((btn) => {
        btn.addEventListener("click", function () {
          const filter = this.getAttribute("data-filter").toLowerCase();

          // Update active button
          quoteFilterButtons.forEach((b) => b.classList.remove("active"));
          this.classList.add("active");

          // Filter rows
          const rows = quotesTable.querySelectorAll("tr");
          rows.forEach((row) => {
            if (filter === "all") {
              row.style.display = "";
            } else {
              const status = (
                row.getAttribute("data-status") || ""
              ).toLowerCase();
              // Handle status variations
              let matches = false;
              if (filter === "pendingpmapproval" || filter === "pending") {
                matches =
                  status.includes("pending") || status.includes("draft");
              } else if (filter === "senttoclient" || filter === "sent") {
                matches = status.includes("sent");
              } else if (filter === "approved") {
                matches =
                  status.includes("approved") ||
                  status.includes("senttoclient");
              } else if (filter === "rejected" || filter === "pmrejected") {
                matches =
                  status.includes("rejected") || status.includes("declined");
              } else {
                matches = status.includes(filter);
              }
              row.style.display = matches ? "" : "none";
            }
          });
        });
      });
    }
  }

  // Invoice filters
  const invoicesTable = document.getElementById("invoicesTableBody");
  if (invoicesTable) {
    const trackingCard = invoicesTable.closest(".tracking-card");
    if (trackingCard) {
      const invoiceFilterButtons = trackingCard.querySelectorAll(
        ".tracking-header .filter-btn"
      );
      invoiceFilterButtons.forEach((btn) => {
        btn.addEventListener("click", function () {
          const filter = this.getAttribute("data-filter").toLowerCase();

          // Update active button
          invoiceFilterButtons.forEach((b) => b.classList.remove("active"));
          this.classList.add("active");

          // Filter rows
          const rows = invoicesTable.querySelectorAll("tr");
          rows.forEach((row) => {
            if (filter === "all") {
              row.style.display = "";
            } else if (filter === "overdue") {
              const isOverdue = row.getAttribute("data-overdue") === "true";
              row.style.display = isOverdue ? "" : "none";
            } else {
              const status = row.getAttribute("data-status") || "";
              row.style.display = status.includes(filter) ? "" : "none";
            }
          });
        });
      });
    }
  }
}
