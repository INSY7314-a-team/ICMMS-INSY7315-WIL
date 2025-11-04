document.addEventListener("DOMContentLoaded", function () {
  const quotationRejectionModal = new bootstrap.Modal(
    document.getElementById("quotationRejectionModal")
  );
  let currentQuotationId = null;
  let currentInvoiceId = null;
  let currentTaskId = null;
  let currentPhaseId = null;
  

  // Handle showing the rejection modal
  document.body.addEventListener("click", function (e) {
    if (e.target.matches(".reject-quote-btn")) {
      currentQuotationId = e.target.dataset.quotationId;
      document.getElementById("rejectionQuotationId").value =
        currentQuotationId;
      quotationRejectionModal.show();
    }
  });

  // Handle quotation approval
  document.body.addEventListener("click", async function (e) {
    if (e.target.matches(".approve-quote-btn")) {
      const quotationId = e.target.dataset.quotationId;
      if (!quotationId) return;

      if (!confirm("Are you sure you want to approve this quotation?")) {
        return;
      }

      const button = e.target;
      const originalText = button.innerHTML;
      button.disabled = true;
      button.innerHTML =
        '<span class="spinner-border spinner-border-sm me-1"></span>Approving...';

      try {
        const response = await fetch("/Clients/ApproveQuotation", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ quotationId: quotationId }),
        });

        const result = await response.json();

        if (result.success) {
          alert("Quotation approved successfully.");
          location.reload();
        } else {
          alert(
            "Failed to approve quotation: " + (result.error || "Unknown error")
          );
          button.disabled = false;
          button.innerHTML = originalText;
        }
      } catch (error) {
        console.error("Error approving quotation:", error);
        alert("An error occurred. Please try again.");
        button.disabled = false;
        button.innerHTML = originalText;
      }
    }
  });

  // Handle submitting the rejection reason
  document
    .getElementById("submitRejectionBtn")
    .addEventListener("click", async function () {
      const reason = document.getElementById("rejectionReason").value;
      if (!reason) {
        alert("Rejection reason is required.");
        return;
      }

      const payload = {
        quotationId: currentQuotationId,
        reason: reason,
      };

      try {
        const response = await fetch("/Clients/RejectQuotation", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(payload),
        });

        const result = await response.json();

        if (result.success) {
          alert("Quotation rejected successfully.");
          quotationRejectionModal.hide();
          location.reload();
        } else {
          alert(
            "Failed to reject quotation: " + (result.error || "Unknown error")
          );
        }
      } catch (error) {
        console.error("Error rejecting quotation:", error);
        alert("An error occurred. Please try again.");
      }
    });

  // Handle task details button clicks
  document.body.addEventListener("click", function (e) {
    if (
      e.target.closest(".view-phases-button") &&
      e.target.closest("[data-task-id]")
    ) {
      const button = e.target.closest(".view-phases-button");
      const taskId = button.getAttribute("data-task-id");
      currentTaskId = taskId;
      loadTaskDetails(taskId);
    }
  });

  // Handle phase details button clicks
  document.body.addEventListener("click", function (e) {
    if (
      e.target.closest(".view-phases-button") &&
      e.target.closest("[data-phase-id]")
    ) {
      const button = e.target.closest(".view-phases-button");
      const phaseId = button.getAttribute("data-phase-id");
      currentPhaseId = phaseId;
      loadPhaseDetails(phaseId);
    }
  });

  // Handle quotation details clicks
  document.body.addEventListener("click", function (e) {
    if (e.target.closest(".clickable-quote")) {
      const quoteElement = e.target.closest(".clickable-quote");
      const quotationId = quoteElement.getAttribute("data-quotation-id");
      currentQuotationId = quotationId;
      loadQuotationDetails(quotationId);
    }
  });

  // Handle invoice details clicks
  document.body.addEventListener("click", function (e) {
    if (e.target.closest(".clickable-invoice")) {
      const invoiceElement = e.target.closest(".clickable-invoice");
      const invoiceId = invoiceElement.getAttribute("data-invoice-id");
      currentInvoiceId = invoiceId;
      loadInvoiceDetails(invoiceId);
    }
  });

  function loadTaskDetails(taskId) {
    const content = document.getElementById("taskDetailsContent");

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading task details...</p>
      </div>
    `;

    // Find the task data from the global data
    const task = window.projectData?.tasks?.find(
      (t) => t.taskId == taskId || t.taskId === taskId
    );

    if (task) {
      content.innerHTML = generateTaskDetailsContent(task);
    } else {
      content.innerHTML = `
        <div class="alert alert-danger">
          <i class="fa-solid fa-exclamation-triangle me-2"></i>
          Task not found. ID: ${taskId}
          <br><small>Available task IDs: ${
            window.projectData?.tasks?.map((t) => t.taskId).join(", ") || "None"
          }</small>
        </div>
      `;
    }
  }

  function loadPhaseDetails(phaseId) {
    const content = document.getElementById("phaseDetailsContent");

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading phase details...</p>
      </div>
    `;

    // Find the phase data from the global data
    const phase = window.projectData?.phases?.find(
      (p) => p.phaseId == phaseId || p.phaseId === phaseId
    );

    if (phase) {
      content.innerHTML = generatePhaseDetailsContent(phase);
    } else {
      content.innerHTML = `
        <div class="alert alert-danger">
          <i class="fa-solid fa-exclamation-triangle me-2"></i>
          Phase not found. ID: ${phaseId}
          <br><small>Available phase IDs: ${
            window.projectData?.phases?.map((p) => p.phaseId).join(", ") ||
            "None"
          }</small>
        </div>
      `;
    }
  }

  function loadQuotationDetails(quotationId) {
    // Store quotation ID globally for download button
    window.currentQuotationId = quotationId;
    
    const content = document.getElementById("quoteDetailsContent");
    const modal = new bootstrap.Modal(
      document.getElementById("quoteDetailsModal")
    );

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading quotation details...</p>
      </div>
    `;

    // Show the modal
    modal.show();

    // Fetch quotation details from the server
    fetch(`/Clients/GetQuotationDetails?id=${quotationId}`)
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          // The API returns { quotation: {...}, items: [...] }
          const quotation = data.data?.quotation || {};
          const items = data.data?.items || [];

          // Merge the items into the quotation object for easier access
          quotation.items = items;

          content.innerHTML = generateQuotationDetailsContent(quotation);
          setupQuotationActions(quotation);
        } else {
          content.innerHTML = `
            <div class="alert alert-danger">
              <i class="fa-solid fa-exclamation-triangle me-2"></i>
              Failed to load quotation details: ${data.error || "Unknown error"}
            </div>
          `;
        }
      })
      .catch((error) => {
        console.error("Error loading quotation details:", error);
        content.innerHTML = `
          <div class="alert alert-danger">
            <i class="fa-solid fa-exclamation-triangle me-2"></i>
            An error occurred while loading quotation details. Please try again.
          </div>
        `;
      });
  }

  function loadInvoiceDetails(invoiceId) {
    const content = document.getElementById("invoiceDetailsContent");
    const modal = new bootstrap.Modal(
      document.getElementById("invoiceDetailsModal")
    );

    // Show loading state
    content.innerHTML = `
      <div class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-2 text-muted">Loading invoice details...</p>
      </div>
    `;

    // Show the modal
    modal.show();

    // Fetch invoice partial from the server and inject HTML
    fetch(`/Clients/InvoiceDetailsPartial?id=${encodeURIComponent(invoiceId)}`)
      .then((response) => {
        if (!response.ok) {
          throw new Error("Failed to load invoice details");
        }
        return response.text();
      })
      .then((html) => {
        content.innerHTML = html;
      })
      .catch((error) => {
        console.error("Error loading invoice details:", error);
        content.innerHTML = `
          <div class="alert alert-danger">
            <i class="fa-solid fa-exclamation-triangle me-2"></i>
            An error occurred while loading invoice details. Please try again.
          </div>
        `;
      });

    const payBtn = document.getElementById("payInvoiceBtn");
    if (payBtn) {
      payBtn.onclick = function () {
        if (confirm("Are you sure you want to mark this invoice as paid?")) {
          payInvoice(invoiceId);
        }
      };
    }
  }


  // Pay invoice function:
  function payInvoice(invoiceId, callback) {
    fetch(`/Clients/PayInvoice/${invoiceId}`, {
        method: "POST"
    })
    .then(res => res.json())
    .then(data => {
        if (callback) callback(data.success);
    })
    .catch(err => {
        console.error(err);
        if (callback) callback(false);
    });
}


  function generateTaskDetailsContent(task) {
    const startDate = new Date(task.startDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const dueDate = new Date(task.dueDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const phaseName =
      window.projectData?.phases?.find((p) => p.phaseId === task.phaseId)
        ?.name || "Unknown Phase";
    const contractorName =
      window.projectData?.contractors?.find((c) => c.userId === task.assignedTo)
        ?.fullName || "Unassigned";

    return `
      <div class="task-details-container">
        <!-- Task Header -->
        <div class="row mb-4">
          <div class="col-md-8">
            <h4 class="mb-2 text-white">${task.name}</h4>
            <p class="text-muted mb-0">Task for project</p>
          </div>
          <div class="col-md-4 text-end">
            <div class="mb-2">
              <span class="badge ${getTaskStatusBadgeClass(
                task.status
              )} me-2">${task.status}</span>
              <span class="badge ${getPriorityBadgeClass(
                task.priority
              )} me-2">${task.priority}</span>
            </div>
            <small class="text-muted">Phase: ${phaseName}</small>
          </div>
        </div>
        
        <!-- Task Summary -->
        <div class="row mb-4">
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-calendar-alt me-2" style="color: #0dcaf0;"></i>Dates
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold text-white">${startDate} - ${dueDate}</div>
                <small class="text-muted">Start Date - Due Date</small>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-chart-line me-2" style="color: #f7ec59;"></i>Progress
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold fs-4" style="color: #f7ec59;">${
                  task.progress
                }%</div>
                <small class="text-muted">Overall task progress</small>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Description -->
        <div class="row mb-4">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-align-left me-2" style="color: #f7ec59;"></i>Description
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <p class="text-white">${
                  task.description || "No description provided."
                }</p>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Additional Information -->
        <div class="row">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-info-circle me-2" style="color: #f7ec59;"></i>Additional Information
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-md-6">
                    <small class="text-muted">Task ID</small>
                    <div class="fw-bold font-monospace text-white">${
                      task.taskId
                    }</div>
                  </div>
                  <div class="col-md-6">
                    <small class="text-muted">Assigned To</small>
                    <div class="fw-bold text-white">${contractorName}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  function generatePhaseDetailsContent(phase) {
    const startDate = new Date(phase.startDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const endDate = new Date(phase.endDate).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
    const progress = phase.progress || 0;

    return `
      <div class="phase-details-container">
        <!-- Phase Header -->
        <div class="row mb-4">
          <div class="col-md-8">
            <h4 class="mb-2 text-white">${phase.name}</h4>
            <p class="text-muted mb-0">Phase for project</p>
          </div>
          <div class="col-md-4 text-end">
            <div class="mb-2">
              <span class="badge ${getStatusBadgeClass(
                phase.status
              )} me-2">${phase.status}</span>
            </div>
            <small class="text-muted">Created: ${new Date(
              phase.createdAt || new Date()
            ).toLocaleDateString("en-US", {
              year: "numeric",
              month: "short",
              day: "numeric",
            })}</small>
          </div>
        </div>
        
        <!-- Phase Summary -->
        <div class="row mb-4">
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-calendar-alt me-2" style="color: #0dcaf0;"></i>Dates
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold text-white">${startDate} - ${endDate}</div>
                <small class="text-muted">Start Date - End Date</small>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-chart-line me-2" style="color: #f7ec59;"></i>Progress
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="fw-bold fs-4" style="color: #f7ec59;">${progress}%</div>
                <small class="text-muted">Overall phase progress</small>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Description -->
        <div class="row mb-4">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-align-left me-2" style="color: #f7ec59;"></i>Description
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <p class="text-white">${
                  phase.description || "No description provided."
                }</p>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Additional Information -->
        <div class="row">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-info-circle me-2" style="color: #f7ec59;"></i>Additional Information
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-md-6">
                    <small class="text-muted">Phase ID</small>
                    <div class="fw-bold font-monospace text-white">${
                      phase.phaseId
                    }</div>
                  </div>
                  <div class="col-md-6">
                    <small class="text-muted">Progress</small>
                    <div class="fw-bold text-white">${progress}%</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  function getStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-warning";
      case "in progress":
        return "bg-primary";
      case "awaiting approval":
        return "bg-info";
      case "completed":
        return "bg-success";
      case "cancelled":
        return "bg-danger";
      case "draft":
        return "badge-secondary";
      case "planning":
        return "badge-info";
      case "active":
        return "badge-primary";
      case "maintenance":
        return "badge-warning";
      default:
        return "bg-secondary";
    }
  }

  function getTaskStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-secondary";
      case "in progress":
      case "inprogress":
      case "in-progress":
        return "bg-warning";
      case "awaiting approval":
      case "awaiting-approval":
      case "awaitingapproval":
        return "bg-info";
      case "completed":
        return "bg-success";
      case "overdue":
        return "bg-danger";
      default:
        return "bg-light";
    }
  }

  function getPriorityBadgeClass(priority) {
    switch (priority.toLowerCase()) {
      case "high":
        return "bg-danger";
      case "medium":
        return "bg-warning";
      case "low":
        return "bg-success";
      default:
        return "bg-secondary";
    }
  }

  function generateQuotationDetailsContent(quotation) {
    const createdDate = new Date(quotation.createdAt).toLocaleDateString(
      "en-US",
      {
        year: "numeric",
        month: "short",
        day: "numeric",
      }
    );

    return `
      <div class="quotation-details-container">
        <!-- Quotation Header -->
        <div class="row mb-4">
          <div class="col-md-8">
            <h4 class="mb-2 text-white">Quotation #${quotation.quotationId}</h4>
            <p class="text-muted mb-0">Project quotation details</p>
          </div>
          <div class="col-md-4 text-end">
            <div class="mb-2">
              <span class="badge ${getQuotationStatusBadgeClass(
                quotation.status || "Unknown"
              )} me-2">${getQuotationStatusDisplayText(quotation.status)}</span>
            </div>
            <small class="text-muted">Created: ${createdDate}</small>
          </div>
        </div>
        
        <!-- Quotation Summary -->
        <div class="row mb-4">
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-money-bill-wave me-2" style="color: #f7ec59;"></i>Financial Summary
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-6">
                    <small class="text-muted">Subtotal</small>
                    <div class="fw-bold text-white">R ${
                      quotation.subtotal?.toLocaleString() || "0"
                    }</div>
                  </div>
                  <div class="col-6">
                    <small class="text-muted">VAT (15%)</small>
                    <div class="fw-bold text-white">R ${
                      quotation.taxTotal?.toLocaleString() || "0"
                    }</div>
                  </div>
                </div>
                <hr style="border-color: rgba(255, 255, 255, 0.2);">
                <div class="fw-bold fs-4" style="color: #f7ec59;">R ${
                  quotation.grandTotal?.toLocaleString() ||
                  quotation.total?.toLocaleString() ||
                  "0"
                }</div>
                <small class="text-muted">Total Amount</small>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-info-circle me-2" style="color: #f7ec59;"></i>Quotation Info
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="mb-2">
                  <small class="text-muted">Quotation ID</small>
                  <div class="fw-bold font-monospace text-white">${
                    quotation.quotationId
                  }</div>
                </div>
                <div class="mb-2">
                  <small class="text-muted">Status</small>
                  <div class="fw-bold text-white">${getQuotationStatusDisplayText(
                    quotation.status
                  )}</div>
                </div>
                <div>
                  <small class="text-muted">Created</small>
                  <div class="fw-bold text-white">${createdDate}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Quotation Items -->
        <div class="row mb-4">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-list me-2" style="color: #f7ec59;"></i>Quotation Items
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body p-0">
                ${
                  quotation.items && quotation.items.length > 0
                    ? `
                  <div class="table-responsive">
                    <table class="table table-dark table-hover mb-0">
                      <thead>
                        <tr>
                          <th>Description</th>
                          <th>Quantity</th>
                          <th>Unit Price</th>
                          <th>Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        ${quotation.items
                          .map(
                            (item) => `
                          <tr>
                            <td>${item.name || item.description || "N/A"}</td>
                            <td>${item.quantity || "0"}</td>
                            <td>R ${(item.unitPrice || 0).toLocaleString()}</td>
                            <td>R ${(item.lineTotal || 0).toLocaleString()}</td>
                          </tr>
                        `
                          )
                          .join("")}
                      </tbody>
                    </table>
                  </div>
                `
                    : `
                  <div class="text-center py-4">
                    <i class="fa-solid fa-list fa-2x text-muted mb-2"></i>
                    <p class="text-muted mb-0">No items found for this quotation</p>
                  </div>
                `
                }
              </div>
            </div>
          </div>
        </div>
        
        <!-- Additional Information -->
        <div class="row">
          <div class="col-12">
            <h6 class="mb-3 text-white">
              <i class="fa-solid fa-file-alt me-2" style="color: #f7ec59;"></i>Additional Information
            </h6>
            <div class="card" style="background: #2a2b35; border: 1px solid rgba(255, 255, 255, 0.1);">
              <div class="card-body">
                <div class="row">
                  <div class="col-md-6">
                    <small class="text-muted">Description</small>
                    <div class="fw-bold text-white">${
                      quotation.description || "No description provided"
                    }</div>
                  </div>
                  <div class="col-md-6">
                    <small class="text-muted">Valid Until</small>
                    <div class="fw-bold text-white">${
                      quotation.validUntil
                        ? new Date(quotation.validUntil).toLocaleDateString(
                            "en-US",
                            {
                              year: "numeric",
                              month: "short",
                              day: "numeric",
                            }
                          )
                        : "Not specified"
                    }</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
  }

  function getQuotationStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "bg-warning";
      case "senttoclient":
        return "bg-info";
      case "clientaccepted":
        return "bg-success";
      case "approved":
        return "bg-success";
      case "accepted":
        return "bg-success";
      case "rejected":
        return "bg-danger";
      case "draft":
        return "bg-secondary";
      default:
        return "bg-info";
    }
  }

  function getQuotationStatusDisplayText(status) {
    switch (status.toLowerCase()) {
      case "pending":
        return "Pending";
      case "senttoclient":
        return "Pending Client Decision";
      case "clientaccepted":
        return "Accepted";
      case "approved":
        return "Approved";
      case "rejected":
        return "Rejected";
      case "draft":
        return "Draft";
      default:
        return status || "Unknown";
    }
  }

  function setupQuotationActions(quotation) {
    const actionButtons = document.getElementById("quoteActionButtons");
    const approveBtn = document.getElementById("approveQuoteBtn");
    const rejectBtn = document.getElementById("rejectQuoteBtn");

    // Show action buttons for pending quotations or quotations sent to client
    if (
      quotation.status &&
      (quotation.status.toLowerCase() === "pending" ||
        quotation.status.toLowerCase() === "senttoclient")
    ) {
      actionButtons.style.display = "block";

      // Set up approve button
      approveBtn.onclick = function () {
        if (confirm("Are you sure you want to approve this quotation?")) {
          approveQuotation(quotation.quotationId);
        }
      };

      // Set up reject button
      rejectBtn.onclick = function () {
        currentQuotationId = quotation.quotationId;
        document.getElementById("rejectionQuotationId").value =
          quotation.quotationId;
        quotationRejectionModal.show();
      };
    } else {
      actionButtons.style.display = "none";
    }
  }

  async function approveQuotation(quotationId) {
    const approveBtn = document.getElementById("approveQuoteBtn");
    const originalText = approveBtn.innerHTML;

    approveBtn.disabled = true;
    approveBtn.innerHTML =
      '<span class="spinner-border spinner-border-sm me-1"></span>Approving...';

    try {
      const response = await fetch("/Clients/ApproveQuotation", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ quotationId: quotationId }),
      });

      const result = await response.json();

      if (result.success) {
        alert("Quotation approved successfully.");
        location.reload();
      } else {
        alert(
          "Failed to approve quotation: " + (result.error || "Unknown error")
        );
        approveBtn.disabled = false;
        approveBtn.innerHTML = originalText;
      }
    } catch (error) {
      console.error("Error approving quotation:", error);
      alert("An error occurred. Please try again.");
      approveBtn.disabled = false;
      approveBtn.innerHTML = originalText;
    }
  }

  // Global function for downloading invoices
  window.downloadInvoice = async function (invoiceId) {
    if (!invoiceId) {
      console.error("❌ No invoice ID provided");
      alert("Invoice ID missing — cannot download.");
      return;
    }

    console.log("📥 Starting invoice download for:", invoiceId);
    console.log(
      "📥 Download URL:",
      `/Clients/DownloadInvoice/${encodeURIComponent(invoiceId)}`
    );

    try {
      const res = await fetch(
        `/Clients/DownloadInvoice/${encodeURIComponent(invoiceId)}`,
        {
          method: "GET",
          headers: {
            Accept: "application/pdf",
            "X-Requested-With": "XMLHttpRequest",
          },
        }
      );

      if (!res.ok) {
        console.error(`⚠️ Download failed (${res.status}):`, res.statusText);
        const errorText = await res.text();
        console.error("Error details:", errorText);
        alert(`Failed to download invoice (${res.status}): ${res.statusText}`);
        return;
      }

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);

      const a = document.createElement("a");
      a.href = url;
      a.download = `Invoice_${invoiceId}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();

      window.URL.revokeObjectURL(url);
      console.log("✅ Invoice downloaded successfully!");
    } catch (err) {
      console.error("🔥 Error downloading invoice:", err);
      alert("An unexpected error occurred while downloading the invoice.");
    }
  };

  // Global function for paying invoices
  window.payInvoice = async function (invoiceId) {
    if (!invoiceId) {
      console.error("❌ No invoice ID provided");
      alert("Invoice ID missing — cannot process payment.");
      return;
    }

    console.log("💳 Starting payment processing for invoice:", invoiceId);

    const payBtn = document.getElementById("payInvoiceBtn");
    if (payBtn) {
      payBtn.disabled = true;
      payBtn.innerHTML =
        '<span class="spinner-border spinner-border-sm me-1"></span>Processing...';
    }

    try {
      const response = await fetch("/Clients/PayInvoice", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Requested-With": "XMLHttpRequest",
        },
        body: JSON.stringify({
          InvoiceId: invoiceId,
          PaymentMethod: "Online",
          Notes: "Payment made via client portal",
        }),
      });

      const result = await response.json();

      if (result.success) {
        alert(
          "Payment processed successfully! The invoice has been marked as paid."
        );
        // Close the modal and reload the page to show updated status
        const modal = bootstrap.Modal.getInstance(
          document.getElementById("invoiceDetailsModal")
        );
        if (modal) {
          modal.hide();
        }
        location.reload();
      } else {
        alert(`Payment failed: ${result.error || "Unknown error"}`);
        if (payBtn) {
          payBtn.disabled = false;
          payBtn.innerHTML =
            '<i class="fas fa-credit-card" style="padding-right: 10px;"></i>Pay Now';
        }
      }
    } catch (err) {
      console.error("🔥 Error processing payment:", err);
      alert("An unexpected error occurred while processing payment.");
      if (payBtn) {
        payBtn.disabled = false;
        payBtn.innerHTML =
          '<i class="fas fa-credit-card" style="padding-right: 10px;"></i>Pay Now';
      }
    }
  };

  
});
