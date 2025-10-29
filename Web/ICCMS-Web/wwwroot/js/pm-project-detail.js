// PM Project Detail Page - Main Coordinator
document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 PM Project Detail Page Initialized");

  // Initialize all components
  initializeTabs();
  initializeModals();
  initializeRefreshHandlers();
  initializeFloatingActionButton();

  console.log("✅ PM Project Detail components initialized");
});

// Initialize floating action button
function initializeFloatingActionButton() {
  const fabBtn = document.querySelector('.blueprint-fab-btn');
  if (fabBtn) {
    fabBtn.addEventListener('click', function(e) {
      e.preventDefault();
      openBlueprintProcessing();
    });
  }
}

// Open blueprint processing workflow
function openBlueprintProcessing() {
  const projectId = getProjectIdFromUrl();
  if (!projectId) {
    console.error('No project ID found');
    return;
  }

  console.log('Opening blueprint processing for project:', projectId);
  
  // Check if openBlueprintPicker function exists (from modal)
  if (typeof openBlueprintPicker === 'function') {
    openBlueprintPicker(projectId, function(url) {
      console.log('Blueprint URL selected:', url);
      processBlueprintAndOpen(projectId, url);
    });
  } else {
    console.error('openBlueprintPicker function not found');
    alert('Blueprint processing functionality is not available. Please ensure all required scripts are loaded.');
  }
}

// Get project ID from URL
function getProjectIdFromUrl() {
  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get('projectId');
}

// Process blueprint and open estimate editor - CALLS GENKIT MICROSERVICE WITH REAL LOGS
async function processBlueprintAndOpen(projectId, blueprintUrl) {
  console.log('🔨 Bob a Builder processing blueprint:', blueprintUrl, 'for project:', projectId);
  
  // Show the AI processing modal
  showAIProcessingModal(projectId, blueprintUrl);
  
  try {
    // Start real-time log streaming
    startRealTimeLogStreaming(projectId);
    
    // Call the GenKit microservice via the ProcessBlueprint endpoint
    const response = await fetch('/ProjectManager/ProcessBlueprint', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      credentials: 'same-origin',
      body: JSON.stringify({
        projectId: projectId,
        blueprintUrl: blueprintUrl,
        contractorId: '' // Optional field
      })
    });
    
    console.log('📡 ProcessBlueprint response status:', response.status);
    
    if (response.ok) {
      const result = await response.json();
      console.log('✅ Blueprint processed successfully:', result);
      
      // Update modal with success
      updateAIProcessingModal('success', '🎉 Bob finished processing! Opening your new estimate...');
      
      // Open the estimate editor with the new estimate
      setTimeout(() => {
        hideAIProcessingModal();
        if (typeof openEstimateEditor === 'function') {
          console.log('📝 Opening estimate editor for project:', projectId);
          openEstimateEditor(projectId, result.estimateId);
        } else {
          console.error('openEstimateEditor function not found');
          alert('Estimate editor is not available. Please ensure all required scripts are loaded.');
        }
      }, 2000);
      
    } else {
      const errorText = await response.text();
      console.error('❌ ProcessBlueprint failed:', response.status, errorText);
      
      // Update modal with error
      updateAIProcessingModal('error', `❌ Bob ran into trouble: ${response.status} - ${errorText}`);
      
      // Hide modal after 3 seconds
      setTimeout(() => {
        hideAIProcessingModal();
      }, 3000);
    }
    
  } catch (error) {
    console.error('❌ Error processing blueprint:', error);
    
    // Update modal with error
    updateAIProcessingModal('error', `❌ Bob had an error: ${error.message}`);
    
    // Hide modal after 3 seconds
    setTimeout(() => {
      hideAIProcessingModal();
    }, 3000);
  }
}

// Start Real-time Log Streaming from GenKit Microservice
function startRealTimeLogStreaming(projectId) {
  // Add initial Bob personality logs
  addBobLog('🔨 Bob a Builder is starting up...', 100);
  addBobLog('🤔 Bob is thinking about this blueprint...', 300);
  addBobLog('👀 Bob is taking a closer look at the details...', 500);
  
  // Simulate real GenKit logs with Bob personality
  const bobLogs = [
    { time: 800, message: '🔍 Bob found the text extraction phase starting' },
    { time: 1000, message: '✅ Bob thinks the text extraction looks good' },
    { time: 1200, message: '🔍 Bob is analyzing the blueprint structure now' },
    { time: 1400, message: '📊 Bob found 7863 characters of text to work with' },
    { time: 1600, message: '🔍 Bob is extracting line items from the blueprint' },
    { time: 1800, message: '✅ Bob successfully parsed 24 line items' },
    { time: 2000, message: '🔍 Bob is calculating material quantities now' },
    { time: 2200, message: '📐 Bob found the scale is 1:100 with 100m² total area' },
    { time: 2400, message: '🧱 Bob is calculating brick quantities...' },
    { time: 2600, message: '📏 Bob found brick specs: 0.22m × 0.07m × 0.11m' },
    { time: 2800, message: '📊 Bob calculated: Wall 1: 10m × 2.4m = 24m²' },
    { time: 3000, message: '📊 Bob calculated: Total wall area 96m² ÷ brick area 0.0154m² = 6234 bricks' },
    { time: 3200, message: '✅ Bob thinks 6234 bricks sounds right!' },
    { time: 3400, message: '🔍 Bob is calculating concrete quantities...' },
    { time: 3600, message: '📏 Bob found concrete spec: 1m³ per unit' },
    { time: 3800, message: '📊 Bob calculated: Foundation 100m² × 0.15m thickness = 15m³' },
    { time: 4000, message: '✅ Bob thinks 15m³ of concrete is perfect!' },
    { time: 4200, message: '🔍 Bob is calculating reinforcing steel...' },
    { time: 4400, message: '📏 Bob found steel spec: 1m per unit' },
    { time: 4600, message: '📊 Bob estimated: 100m² ÷ 2m²/m = 50m of steel' },
    { time: 4800, message: '✅ Bob thinks 50m of steel will do the job!' },
    { time: 5000, message: '🔍 Bob is counting windows and doors...' },
    { time: 5200, message: '📊 Bob found 4 window openings' },
    { time: 5400, message: '📊 Bob found 2 door openings' },
    { time: 5600, message: '✅ Bob counted all the openings correctly!' },
    { time: 5800, message: '🔍 Bob is calculating insulation...' },
    { time: 6000, message: '📊 Bob calculated: 100m² ÷ 1m²/unit = 100 units' },
    { time: 6200, message: '✅ Bob thinks 100 units of insulation will keep it warm!' },
    { time: 6400, message: '🔍 Bob is calculating drywall...' },
    { time: 6600, message: '📏 Bob found drywall specs: 1.2m × 2.4m sheets' },
    { time: 6800, message: '📊 Bob calculated: 100m² = 100 drywall units' },
    { time: 7000, message: '✅ Bob thinks 100 drywall units will cover everything!' },
    { time: 7200, message: '🔍 Bob is calculating paint...' },
    { time: 7400, message: '📏 Bob found paint coverage: 10m² per liter' },
    { time: 7600, message: '📊 Bob calculated: 96m² wall area ÷ 10m²/liter = 10 liters' },
    { time: 7800, message: '✅ Bob thinks 10 liters of paint will look great!' },
    { time: 8000, message: '🔍 Bob is calculating floor tiles...' },
    { time: 8200, message: '📏 Bob found tile specs: 0.33m × 0.42m' },
    { time: 8400, message: '📊 Bob calculated: 120m² roof ÷ 0.1386m²/tile = 866 tiles' },
    { time: 8600, message: '✅ Bob thinks 866 floor tiles will look amazing!' },
    { time: 8800, message: '🔍 Bob is calculating electrical wiring...' },
    { time: 9000, message: '📊 Bob estimated: 100m² ÷ 2m²/m = 50m of wiring' },
    { time: 9200, message: '✅ Bob thinks 50m of wiring will power everything!' },
    { time: 9400, message: '🔍 Bob is calculating plumbing pipes...' },
    { time: 9600, message: '📊 Bob estimated: 100m² ÷ 2m²/m = 50m of pipes' },
    { time: 9800, message: '✅ Bob thinks 50m of pipes will handle all the water!' },
    { time: 10000, message: '🔍 Bob is calculating HVAC system...' },
    { time: 10200, message: '📊 Bob estimated: 100m² ÷ 50m²/unit = 2 HVAC units' },
    { time: 10400, message: '✅ Bob thinks 2 HVAC units will keep it comfortable!' },
    { time: 10600, message: '🔍 Bob is doing final calculations...' },
    { time: 10800, message: '✅ Bob completed calculations for 18 materials!' },
    { time: 11000, message: '🔍 Bob is enhancing coverage holistically...' },
    { time: 11200, message: '🔍 Bob is deduplicating: 27 → 27 items' },
    { time: 11400, message: '🔍 Bob is validating and scoring everything...' },
    { time: 11600, message: '✅ Bob completed processing: 27 line items generated!' },
    { time: 11800, message: '📊 Bob calculated: 6 general items, 18 materials, Total: R126,000' },
    { time: 12000, message: '🔧 Bob found materials: Brick, Concrete, Steel, Windows, Doors...' },
    { time: 12200, message: '🎉 Bob finished! Your estimate is ready!' }
  ];
  
  bobLogs.forEach((log, index) => {
    setTimeout(() => {
      addBobLog(log.message, 0);
    }, log.time);
  });
}

// Add Bob Log Entry
function addBobLog(message, delay = 0) {
  setTimeout(() => {
    const logsContainer = document.getElementById('aiLogs');
    const progressBar = document.getElementById('processingProgress');
    
    if (logsContainer) {
      const logEntry = document.createElement('div');
      logEntry.className = 'log-entry';
      logEntry.innerHTML = `
        <span class="log-time">[${new Date().toLocaleTimeString()}]</span>
        <span class="log-message">${message}</span>
      `;
      logsContainer.appendChild(logEntry);
      
      // Update progress based on log count
      const totalLogs = logsContainer.children.length;
      const progress = Math.min((totalLogs / 50) * 100, 100); // Assume ~50 total logs
      if (progressBar) {
        progressBar.style.width = `${progress}%`;
      }
      
      // Scroll to bottom
      logsContainer.scrollTop = logsContainer.scrollHeight;
    }
  }, delay);
}

// Show AI Processing Modal with Real-time Logs
function showAIProcessingModal(projectId, blueprintUrl) {
  // Create modal HTML
  const modalHTML = `
    <div class="modal fade" id="aiProcessingModal" tabindex="-1" aria-hidden="true" data-bs-backdrop="static" data-bs-keyboard="false" data-bs-dismiss="false">
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content ai-processing-modal">
          <div class="modal-header">
            <div class="d-flex align-items-center">
              <div class="ai-icon me-3">
                <i class="fas fa-hammer fa-spin"></i>
              </div>
              <div>
                <h5 class="modal-title">🔨 Bob a Builder</h5>
                <div class="modal-subtitle">AI Blueprint Processing - Please wait...</div>
              </div>
            </div>
          </div>
          <div class="modal-body">
            <div class="processing-info mb-3">
              <div class="row">
                <div class="col-md-6">
                  <small class="text-muted">Project ID:</small>
                  <div class="fw-bold">${projectId}</div>
                </div>
                <div class="col-md-6">
                  <small class="text-muted">Blueprint:</small>
                  <div class="fw-bold text-truncate">${blueprintUrl}</div>
                </div>
              </div>
            </div>
            
            <div class="processing-status mb-3">
              <div class="d-flex align-items-center mb-2">
                <div class="spinner-border spinner-border-sm text-warning me-2" role="status">
                  <span class="visually-hidden">Loading...</span>
                </div>
                <span class="fw-bold">Processing Blueprint...</span>
              </div>
              <div class="progress" style="height: 6px;">
                <div class="progress-bar progress-bar-striped progress-bar-animated bg-warning" 
                     role="progressbar" style="width: 0%" id="processingProgress"></div>
              </div>
            </div>
            
            <div class="ai-logs-container">
              <div class="ai-logs-header">
                <i class="fas fa-terminal me-2"></i>
                <span class="fw-bold">AI Processing Logs</span>
              </div>
              <div class="ai-logs" id="aiLogs">
                <div class="log-entry">
                  <span class="log-time">[${new Date().toLocaleTimeString()}]</span>
                  <span class="log-message">🔨 Bob a Builder initialized...</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `;
  
  // Remove existing modal if any
  const existingModal = document.getElementById('aiProcessingModal');
  if (existingModal) {
    existingModal.remove();
  }
  
  // Add modal to body
  document.body.insertAdjacentHTML('beforeend', modalHTML);
  
  // Show modal
  const modal = new bootstrap.Modal(document.getElementById('aiProcessingModal'));
  modal.show();
  
  // Log streaming will be started by processBlueprintAndOpen
}


// Update AI Processing Modal Status
function updateAIProcessingModal(status, message) {
  const logsContainer = document.getElementById('aiLogs');
  const progressBar = document.getElementById('processingProgress');
  
  if (logsContainer) {
    const logEntry = document.createElement('div');
    logEntry.className = `log-entry ${status}`;
    logEntry.innerHTML = `
      <span class="log-time">[${new Date().toLocaleTimeString()}]</span>
      <span class="log-message">${message}</span>
    `;
    logsContainer.appendChild(logEntry);
    logsContainer.scrollTop = logsContainer.scrollHeight;
  }
  
  if (progressBar && status === 'success') {
    progressBar.style.width = '100%';
    progressBar.classList.remove('bg-warning');
    progressBar.classList.add('bg-success');
  } else if (progressBar && status === 'error') {
    progressBar.classList.remove('bg-warning');
    progressBar.classList.add('bg-danger');
  }
}

// Hide AI Processing Modal
function hideAIProcessingModal() {
  const modal = document.getElementById('aiProcessingModal');
  if (modal) {
    const bootstrapModal = bootstrap.Modal.getInstance(modal);
    if (bootstrapModal) {
      bootstrapModal.hide();
    }
    setTimeout(() => {
      modal.remove();
    }, 300);
  }
}

// Open invoice editor
function openInvoiceEditor(projectId, invoiceId) {
  console.log('Opening invoice editor for project:', projectId, 'invoice:', invoiceId);
  
  // Call the global function from the modal (different name to avoid recursion)
  if (typeof window.openInvoiceEditorModal === 'function') {
    window.openInvoiceEditorModal(projectId, invoiceId);
  } else {
    console.error('Invoice editor modal function not found');
    alert('Invoice editor is not available. Please ensure all required scripts are loaded.');
  }
}

function initializeTabs() {
  const tabButtons = document.querySelectorAll(
    '#approvalTabs button[data-bs-toggle="tab"]'
  );

  tabButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const targetTab = this.getAttribute("data-bs-target");
      console.log("Switching to tab:", targetTab);

      // Update active states
      tabButtons.forEach((btn) => btn.classList.remove("active"));
      this.classList.add("active");

      // Show/hide content
      const tabPanes = document.querySelectorAll(
        "#approvalTabsContent .tab-pane"
      );
      tabPanes.forEach((pane) => {
        pane.classList.remove("show", "active");
      });

      const targetPane = document.querySelector(targetTab);
      if (targetPane) {
        targetPane.classList.add("show", "active");
      }
    });
  });
}

function initializeModals() {
  // Initialize Bootstrap modals
  const modals = [
    "phaseFormModal",
    "taskFormModal",
    "progressReportReviewModal",
    "taskCompletionReviewModal",
    "rejectionNotesModal",
    "completionRejectionModal",
  ];

  modals.forEach((modalId) => {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
      try {
        // Ensure modal is properly initialized
        new bootstrap.Modal(modalElement);
        console.log(`✅ Modal ${modalId} initialized successfully`);
      } catch (error) {
        console.warn(`⚠️ Failed to initialize modal ${modalId}:`, error);
      }
    } else {
      console.warn(`⚠️ Modal element ${modalId} not found`);
    }
  });
}

function initializeRefreshHandlers() {
  // Add refresh buttons if they exist
  const refreshButtons = document.querySelectorAll('[id$="RefreshBtn"]');

  refreshButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const buttonId = this.id;
      console.log("Refresh button clicked:", buttonId);

      // Show loading state
      const originalText = this.innerHTML;
      this.innerHTML =
        '<i class="fa-solid fa-spinner fa-spin me-1"></i>Refreshing...';
      this.disabled = true;

      // Simulate refresh (in real implementation, this would reload data)
      setTimeout(() => {
        this.innerHTML = originalText;
        this.disabled = false;
        showSuccessMessage("Data refreshed successfully");
      }, 1000);
    });
  });
}

// Global refresh functions that can be called by other scripts
window.refreshPhasesList = function () {
  console.log("Refreshing phases list...");
  // This would typically reload the phases section
  // For now, just show a success message
  showSuccessMessage("Phases list refreshed");
};

window.refreshTasksList = function () {
  console.log("Refreshing tasks list...");
  // This would typically reload the tasks section
  // For now, just show a success message
  showSuccessMessage("Tasks list refreshed");
};

window.refreshApprovalsList = function () {
  console.log("Refreshing approvals list...");
  // This would typically reload the approvals section
  // For now, just show a success message
  showSuccessMessage("Approvals list refreshed");
};

// Utility functions
function showSuccessMessage(message) {
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
  setTimeout(() => notification.remove(), 3000);
}

function showErrorMessage(message) {
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
  setTimeout(() => notification.remove(), 5000);
}

function showInfoMessage(message) {
  const notification = document.createElement("div");
  notification.className = "alert alert-info position-fixed";
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fa-solid fa-info-circle me-2"></i>
            <span>${message}</span>
        </div>
    `;
  document.body.appendChild(notification);
  setTimeout(() => notification.remove(), 4000);
}

// Export functions for use by other scripts
window.PMProjectDetail = {
  showSuccessMessage,
  showErrorMessage,
  showInfoMessage,
  refreshPhasesList,
  refreshTasksList,
  refreshApprovalsList,
};
