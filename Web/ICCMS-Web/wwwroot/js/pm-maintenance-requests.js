// Maintenance Request Management for Project Managers

document.addEventListener("DOMContentLoaded", function () {
    let currentRequestId = null;

    // Handle assign contractor button clicks
    document.body.addEventListener("click", function (e) {
        if (e.target.closest(".assign-contractor-btn")) {
            const btn = e.target.closest(".assign-contractor-btn");
            currentRequestId = btn.getAttribute("data-request-id");
            document.getElementById("assignContractorRequestId").value = currentRequestId;
        }
    });

    // Handle confirm assign contractor
    const confirmAssignBtn = document.getElementById("confirmAssignContractorBtn");
    if (confirmAssignBtn) {
        confirmAssignBtn.addEventListener("click", async function () {
            const requestId = document.getElementById("assignContractorRequestId").value;
            const contractorId = document.getElementById("contractorSelect").value;

            if (!requestId || !contractorId) {
                alert("Please select a contractor.");
                return;
            }

            confirmAssignBtn.disabled = true;
            confirmAssignBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Assigning...';

            try {
                const response = await fetch("/ProjectManager/AssignMaintenanceContractor", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || ''
                    },
                    body: JSON.stringify({
                        requestId: requestId,
                        contractorId: contractorId
                    })
                });

                const result = await response.json();

                if (result.success) {
                    alert("Contractor assigned successfully!");
                    const modal = bootstrap.Modal.getInstance(document.getElementById("assignContractorModal"));
                    if (modal) {
                        modal.hide();
                    }
                    location.reload();
                } else {
                    alert("Failed to assign contractor: " + (result.error || "Unknown error"));
                    confirmAssignBtn.disabled = false;
                    confirmAssignBtn.innerHTML = '<i class="fa-solid fa-check me-1"></i>Assign';
                }
            } catch (error) {
                console.error("Error assigning contractor:", error);
                alert("An error occurred. Please try again.");
                confirmAssignBtn.disabled = false;
                confirmAssignBtn.innerHTML = '<i class="fa-solid fa-check me-1"></i>Assign';
            }
        });
    }

    // Handle approve request - opens wizard (wizard handles submission)
    // No handler needed here as the button opens the modal via data-bs-toggle

    // Handle reject request
    document.body.addEventListener("click", async function (e) {
        if (e.target.closest(".reject-request-btn")) {
            const btn = e.target.closest(".reject-request-btn");
            const requestId = btn.getAttribute("data-request-id");

            if (!confirm("Are you sure you want to reject this maintenance request?")) {
                return;
            }

            btn.disabled = true;
            const originalText = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Rejecting...';

            try {
                const response = await fetch("/ProjectManager/RejectMaintenanceRequest", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || ''
                    },
                    body: JSON.stringify({ requestId: requestId })
                });

                const result = await response.json();

                if (result.success) {
                    alert("Maintenance request rejected.");
                    location.reload();
                } else {
                    alert("Failed to reject request: " + (result.error || "Unknown error"));
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                console.error("Error rejecting request:", error);
                alert("An error occurred. Please try again.");
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        }
    });

    // Handle start work
    document.body.addEventListener("click", async function (e) {
        if (e.target.closest(".start-work-btn")) {
            const btn = e.target.closest(".start-work-btn");
            const requestId = btn.getAttribute("data-request-id");

            if (!confirm("Mark this maintenance request as 'In Progress'?")) {
                return;
            }

            btn.disabled = true;
            const originalText = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Updating...';

            try {
                const response = await fetch("/ProjectManager/StartMaintenanceWork", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || ''
                    },
                    body: JSON.stringify({ requestId: requestId })
                });

                const result = await response.json();

                if (result.success) {
                    alert("Work started successfully.");
                    location.reload();
                } else {
                    alert("Failed to start work: " + (result.error || "Unknown error"));
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                console.error("Error starting work:", error);
                alert("An error occurred. Please try again.");
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        }
    });

    // Handle mark resolved
    document.body.addEventListener("click", async function (e) {
        if (e.target.closest(".mark-resolved-btn")) {
            const btn = e.target.closest(".mark-resolved-btn");
            const requestId = btn.getAttribute("data-request-id");

            if (!confirm("Mark this maintenance request as resolved?")) {
                return;
            }

            btn.disabled = true;
            const originalText = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Updating...';

            try {
                const response = await fetch("/ProjectManager/MarkMaintenanceResolved", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || ''
                    },
                    body: JSON.stringify({ requestId: requestId })
                });

                const result = await response.json();

                if (result.success) {
                    alert("Maintenance request marked as resolved.");
                    location.reload();
                } else {
                    alert("Failed to mark as resolved: " + (result.error || "Unknown error"));
                    btn.disabled = false;
                    btn.innerHTML = originalText;
                }
            } catch (error) {
                console.error("Error marking resolved:", error);
                alert("An error occurred. Please try again.");
                btn.disabled = false;
                btn.innerHTML = originalText;
            }
        }
    });

    // View details button now navigates to full page view (handled by link href)
    // No handler needed here
});

// Show image modal
function showMaintenanceImageModal(imageUrl) {
    document.getElementById("maintenanceImageFull").src = imageUrl;
    const modal = new bootstrap.Modal(document.getElementById("maintenanceImageModal"));
    modal.show();
}

