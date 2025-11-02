// Client Rating Modal Handler
document.addEventListener('DOMContentLoaded', function () {
    initializeRatingModal();
});

let currentContractorId = '';
let currentContractorName = '';

function initializeRatingModal() {
    const rateContractorModal = document.getElementById('rateContractorModal');
    if (!rateContractorModal) return;

    // Handle modal show event
    rateContractorModal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        if (button) {
            currentContractorId = button.getAttribute('data-contractor-id') || '';
            currentContractorName = button.getAttribute('data-contractor-name') || 'Contractor';
            
            // Set hidden fields
            document.getElementById('ratingContractorId').value = currentContractorId;
            document.getElementById('ratingContractorName').value = currentContractorName;
            
            // Reset form
            resetRatingForm();
        }
    });

    // Handle form submission
    const submitBtn = document.getElementById('submitRatingBtn');
    if (submitBtn) {
        submitBtn.addEventListener('click', handleRatingSubmission);
    }

    // Handle star rating selection
    setupStarRating();
}

function resetRatingForm() {
    const form = document.getElementById('rateContractorForm');
    if (form) {
        form.reset();
        // Uncheck all stars
        const radioButtons = form.querySelectorAll('input[type="radio"]');
        radioButtons.forEach(radio => {
            radio.checked = false;
        });
    }
}

function setupStarRating() {
    const stars = document.querySelectorAll('.star-rating input[type="radio"]');
    stars.forEach(star => {
        star.addEventListener('change', function () {
            // Visual feedback is handled by CSS
            console.log('Rating selected:', this.value);
        });
    });
}

async function handleRatingSubmission() {
    const submitBtn = document.getElementById('submitRatingBtn');
    const form = document.getElementById('rateContractorForm');
    
    if (!form || !submitBtn) return;

    // Get selected rating
    const selectedRating = form.querySelector('input[name="rating"]:checked');
    if (!selectedRating) {
        showAlert('Please select a rating before submitting.', 'warning');
        return;
    }

    const contractorId = document.getElementById('ratingContractorId').value;
    const ratingValue = parseInt(selectedRating.value);

    if (!contractorId) {
        showAlert('Contractor information is missing.', 'danger');
        return;
    }

    // Disable submit button and show loading state
    submitBtn.disabled = true;
    const originalHtml = submitBtn.innerHTML;
    submitBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i>Submitting...';

    try {
        const response = await fetch('/Clients/SubmitContractorRating', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                contractorId: contractorId,
                ratingValue: ratingValue
            })
        });

        const result = await response.json();

        if (result.success) {
            showAlert('Thank you! Your rating has been submitted successfully.', 'success');
            
            // Close modal after a brief delay
            setTimeout(() => {
                const modal = bootstrap.Modal.getInstance(document.getElementById('rateContractorModal'));
                if (modal) {
                    modal.hide();
                }
                // Optionally reload the page or update UI
                window.location.reload();
            }, 1500);
        } else {
            showAlert(result.error || 'Failed to submit rating. Please try again.', 'danger');
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalHtml;
        }
    } catch (error) {
        console.error('Error submitting rating:', error);
        showAlert('An error occurred while submitting your rating. Please try again.', 'danger');
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalHtml;
    }
}

function showAlert(message, type) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.rating-alert');
    existingAlerts.forEach(alert => alert.remove());

    // Create new alert
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} rating-alert alert-dismissible fade show`;
    alertDiv.setAttribute('role', 'alert');
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Insert alert at the top of modal body
    const modalBody = document.querySelector('#rateContractorModal .modal-body');
    if (modalBody) {
        modalBody.insertBefore(alertDiv, modalBody.firstChild);
    }
}

// Function to open rating modal (can be called from other scripts)
function openRatingModal(contractorId, contractorName) {
    currentContractorId = contractorId || '';
    currentContractorName = contractorName || 'Contractor';
    
    document.getElementById('ratingContractorId').value = currentContractorId;
    document.getElementById('ratingContractorName').value = currentContractorName;
    resetRatingForm();
    
    const modal = new bootstrap.Modal(document.getElementById('rateContractorModal'));
    modal.show();
}

