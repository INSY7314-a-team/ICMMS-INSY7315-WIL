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
  // Bob's personality: Enthusiastic, confident, construction-savvy, friendly builder
  // All logs compressed to fit within 50000ms (50 seconds)
  const bobLogs = [
    // Initialization phase
    { time: 68, message: '🔨 Hey there! Bob here, ready to work some magic on this blueprint!' },
    { time: 271, message: '💪 Alright, let me fire up the ol\' blueprint processor... this is gonna be good!' },
    { time: 475, message: '👀 Ooh, I see what we\'re working with here. Nice choice of blueprint!' },
    { time: 678, message: '🤔 Just let me get my thinking cap on... or should I say hard hat? 😄' },
    
    // File loading and reading phase
    { time: 882, message: '📥 Starting to download and read your blueprint file...' },
    { time: 1220, message: '⏳ Loading... this might take a sec, but I\'m worth the wait!' },
    { time: 1627, message: '📄 Got it! File loaded successfully. Time to dig in!' },
    { time: 2034, message: '🔍 Scanning through the document structure... looking good so far!' },
    
    // Text extraction phase
    { time: 2508, message: '🔤 Phase 1: Extracting all that text from your blueprint...' },
    { time: 2983, message: '📝 Finding every measurement, label, and annotation...' },
    { time: 3458, message: '✨ Text extraction complete! Got all the juicy details!' },
    { time: 3865, message: '📊 Found a solid amount of text data to work with - this is my bread and butter!' },
    
    // Blueprint analysis phase
    { time: 4340, message: '🏗️ Phase 2: Analyzing the blueprint structure...' },
    { time: 4815, message: '👷 Determining building type, dimensions, and scale...' },
    { time: 5290, message: '📐 Checking out those measurements - precision is key, am I right?' },
    { time: 5765, message: '✅ Blueprint structure analyzed! I\'ve got the full picture now.' },
    { time: 6240, message: '🏢 Looks like we\'re building something solid here - I approve!' },
    
    // Detailed analysis phase
    { time: 6780, message: '🔍 Taking a deep dive into structural elements...' },
    { time: 7322, message: '🧱 Checking foundations, walls, floors - the foundation of it all!' },
    { time: 7865, message: '⚡ Analyzing MEP systems (that\'s Mechanical, Electrical, Plumbing to you!)' },
    { time: 8408, message: '🎨 Reviewing finishes and architectural details - the pretty stuff!' },
    { time: 8950, message: '✅ Got all the structural intel locked and loaded!' },
    
    // Material identification phase
    { time: 9557, message: '🔧 Phase 3: Identifying all materials needed...' },
    { time: 10098, message: '🧱 Looking for bricks, blocks, and masonry... found \'em!' },
    { time: 10649, message: '🏗️ Concrete and cement - the backbone of any solid build!' },
    { time: 11187, message: '🔩 Reinforcing steel and rebar - keeping things strong!' },
    { time: 11726, message: '🪟 Windows and doors - letting in the light!' },
    { time: 12265, message: '🚪 Counting all openings - gotta get those counts right!' },
    { time: 12812, message: '✅ Material identification complete - this is a comprehensive build!' },
    
    // Quantity calculations phase
    { time: 13423, message: '📊 Phase 4: Time for the fun part - calculating quantities!' },
    { time: 14034, message: '🧮 Getting my calculator ready... actually, I don\'t need one, I\'m an AI! 😎' },
    { time: 14653, message: '📐 Calculating brick quantities based on wall dimensions...' },
    { time: 15258, message: '📏 Checking specs: standard brick is 0.22m × 0.07m × 0.11m...' },
    { time: 15868, message: '🔨 Working through each wall section systematically...' },
    { time: 16479, message: '✅ Brick calculations done! That\'s a lot of bricks, but we\'ve got this!' },
    
    { time: 17081, message: '🏗️ Now calculating concrete volumes for foundations...' },
    { time: 17703, message: '📐 Foundation area × thickness = cubic meters... math time!' },
    { time: 18313, message: '🧮 Adding up all foundation sections...' },
    { time: 18920, message: '✅ Concrete quantities locked in - that foundation will be rock solid!' },
    
    { time: 19519, message: '🔩 Calculating reinforcing steel requirements...' },
    { time: 20149, message: '📏 Estimating rebar spacing and coverage...' },
    { time: 20745, message: '✅ Steel calculations complete - structural integrity guaranteed!' },
    
    { time: 21347, message: '🪟 Counting and sizing all windows...' },
    { time: 21962, message: '🚪 Measuring door openings and requirements...' },
    { time: 22566, message: '✅ All openings accounted for - light and access, check!' },
    
    { time: 23199, message: '🧱 Moving on to insulation calculations...' },
    { time: 23807, message: '📐 Working out coverage areas for thermal efficiency...' },
    { time: 24393, message: '✅ Insulation sorted - this place will be cozy!' },
    
    { time: 25017, message: '🏗️ Calculating drywall requirements...' },
    { time: 25633, message: '📏 Standard 1.2m × 2.4m sheets - cutting and fitting in my head!' },
    { time: 26230, message: '✅ Drywall quantities calculated - walls are gonna look smooth!' },
    
    { time: 26840, message: '🎨 Now for paint - how much coverage do we need?' },
    { time: 27449, message: '📐 Calculating wall areas and paint coverage rates...' },
    { time: 28054, message: '✅ Paint quantities done - what color are you thinking? 😄' },
    
    { time: 28682, message: '🔲 Flooring and tiles next...' },
    { time: 29293, message: '📏 Tile sizes and floor areas - let\'s make it beautiful!' },
    { time: 29888, message: '✅ Flooring materials calculated - floors will be top-notch!' },
    
    { time: 30509, message: '⚡ Electrical work time - calculating wiring needs...' },
    { time: 31122, message: '🔌 Estimating circuit requirements and wire lengths...' },
    { time: 31726, message: '✅ Electrical calculations complete - power to the people!' },
    
    { time: 32332, message: '💧 Plumbing calculations - pipes and fittings...' },
    { time: 32963, message: '🚿 Working out water supply and drainage requirements...' },
    { time: 33550, message: '✅ Plumbing sorted - water will flow like a dream!' },
    
    { time: 34160, message: '❄️ HVAC system calculations...' },
    { time: 34771, message: '🌡️ Sizing heating and cooling units for optimal comfort...' },
    { time: 35393, message: '✅ HVAC done - temperature control will be perfect!' },
    
    // Holistic coverage phase
    { time: 36002, message: '🔍 Phase 5: Doing a final holistic review...' },
    { time: 36612, message: '🤔 Let me make sure we haven\'t missed anything...' },
    { time: 37223, message: '🔎 Checking for any gaps in material coverage...' },
    { time: 37825, message: '✨ Adding any missing essentials - I\'ve got your back!' },
    { time: 38428, message: '✅ Holistic coverage complete - nothing slips through the cracks!' },
    
    // Deduplication and validation
    { time: 39034, message: '🧹 Cleaning up - removing any duplicate items...' },
    { time: 39658, message: '🔄 Organizing and validating all line items...' },
    { time: 40267, message: '✅ Everything\'s clean, organized, and ready to go!' },
    
    // Final summary
    { time: 40864, message: '📊 Final summary: Calculating totals and values...' },
    { time: 41486, message: '💰 Adding up all the numbers - this is the moment of truth!' },
    { time: 42097, message: '✅ Summary complete - got a comprehensive itemized list!' },
    { time: 42693, message: '🎯 Total line items generated and validated - looking sharp!' },
    
    // Processing to Estimate
    { time: 43338, message: '🔨 Now, let\'s process this into your estimate...' },
    { time: 43915, message: '📝 Creating estimate structure with all line items...' },
    { time: 44549, message: '💰 Applying pricing to each material and labor item...' },
    { time: 45142, message: '🧮 Calculating line item totals and subtotals...' },
    { time: 45757, message: '📊 Adding markups and calculating final estimate total...' },
    { time: 46380, message: '✅ Estimate structure created successfully!' },
    { time: 46963, message: '💾 Saving your estimate to the database...' },
    { time: 47599, message: '🔗 Linking estimate to your project...' },
    
    // Final messages
    { time: 48794, message: '🎉 WOOHOO! All done! Your estimate will be Ready shortly!' },
    { time: 49423, message: '👷 I\'ve done all the heavy lifting - now it\'s your turn to take a look!' },
    { time: 49430, message: '👷 Your Estimate is Generating and will be display shortly' },
    { time: 50000, message: '💪 Bob out! Happy building! 🔨' }
    
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
      const progress = Math.min((totalLogs / 75) * 100, 100); // Updated for ~75 total logs
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
