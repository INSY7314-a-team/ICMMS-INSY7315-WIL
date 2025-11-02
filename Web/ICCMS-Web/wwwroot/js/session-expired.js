/**
 * Session Expired Modal Handler
 * Handles the display and functionality of the session expired modal
 */

class SessionExpiredHandler {
  constructor() {
    this.modal = null;
    this.countdownInterval = null;
    this.timeLeft = 10; // seconds
    this.isModalVisible = false;
    this.originalTimeLeft = 10;

    this.init();
  }

  init() {
    // Wait for DOM to be ready
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", () => this.setupModal());
    } else {
      this.setupModal();
    }
  }

  setupModal() {
    this.modal = document.getElementById("sessionExpiredModal");
    if (!this.modal) {
      console.warn("Session expired modal not found in DOM");
      return;
    }

    // Setup event listeners
    this.setupEventListeners();

    // Make methods globally available
    window.showSessionExpired = () => this.showModal();
    window.hideSessionExpired = () => this.hideModal();
  }

  setupEventListeners() {
    // Logout now button
    const logoutBtn = document.getElementById("logoutNowBtn");
    if (logoutBtn) {
      logoutBtn.addEventListener("click", () => this.logoutNow());
    }

    // Stay on page button (disabled by default)
    const stayBtn = document.getElementById("stayOnPageBtn");
    if (stayBtn) {
      stayBtn.addEventListener("click", () => this.stayOnPage());
    }

    // Prevent modal from closing when clicking outside
    const backdrop = this.modal.querySelector(".session-expired-backdrop");
    if (backdrop) {
      backdrop.addEventListener("click", (e) => {
        e.stopPropagation();
        // Don't allow closing by clicking backdrop
      });
    }
  }

  showModal() {
    if (this.isModalVisible) {
      return; // Prevent multiple modals
    }

    this.isModalVisible = true;
    this.timeLeft = this.originalTimeLeft;

    // Show modal
    this.modal.style.display = "flex";

    // Start countdown
    this.startCountdown();

    // Start timer animation
    this.startTimerAnimation();

    // Disable page interactions
    this.disablePageInteractions();

    console.log("Session expired modal shown");
  }

  hideModal() {
    if (!this.isModalVisible) {
      return;
    }

    this.isModalVisible = false;

    // Clear countdown
    this.clearCountdown();

    // Hide modal
    this.modal.style.display = "none";

    // Re-enable page interactions
    this.enablePageInteractions();

    console.log("Session expired modal hidden");
  }

  startCountdown() {
    this.updateCountdownDisplay();

    this.countdownInterval = setInterval(() => {
      this.timeLeft--;
      this.updateCountdownDisplay();

      if (this.timeLeft <= 0) {
        this.clearCountdown();
        this.logoutNow();
      }
    }, 1000);
  }

  clearCountdown() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }

  updateCountdownDisplay() {
    const countdownElement = document.getElementById("countdownTimer");
    if (countdownElement) {
      countdownElement.textContent = this.timeLeft;
    }

    // Update progress circle
    this.updateProgressCircle();
  }

  updateProgressCircle() {
    const progressCircle = document.getElementById("timerCircle");
    if (progressCircle) {
      const circumference = 2 * Math.PI * 45; // radius = 45
      const progress =
        (this.originalTimeLeft - this.timeLeft) / this.originalTimeLeft;
      const offset = circumference - progress * circumference;
      progressCircle.style.strokeDashoffset = offset;
    }
  }

  startTimerAnimation() {
    const progressCircle = document.getElementById("timerCircle");
    if (progressCircle) {
      progressCircle.classList.add("animating");
    }
  }

  logoutNow() {
    console.log("User initiated logout");
    this.clearCountdown();
    this.hideModal();

    // Redirect to logout
    window.location.href = "/Auth/Logout";
  }

  stayOnPage() {
    console.log("User chose to stay on page");
    this.clearCountdown();
    this.hideModal();

    // Show a message that functionality will be limited
    this.showLimitedFunctionalityMessage();
  }

  showLimitedFunctionalityMessage() {
    // Create a temporary notification
    const notification = document.createElement("div");
    notification.className = "alert alert-warning position-fixed";
    notification.style.cssText = `
            top: 20px;
            right: 20px;
            z-index: 10000;
            max-width: 300px;
            background: #F7EC59;
            color: #1A1B25;
            border: 2px solid #F7EC59;
            border-radius: 8px;
            padding: 1rem;
            font-weight: 600;
        `;
    notification.innerHTML = `
            <i class="fa-solid fa-exclamation-triangle"></i>
            Session expired. Some features may not work properly.
        `;

    document.body.appendChild(notification);

    // Remove after 5 seconds
    setTimeout(() => {
      if (notification.parentNode) {
        notification.parentNode.removeChild(notification);
      }
    }, 5000);
  }

  disablePageInteractions() {
    // Add a class to body to prevent interactions
    document.body.classList.add("session-expired-active");

    // Disable all form submissions
    const forms = document.querySelectorAll("form");
    forms.forEach((form) => {
      form.addEventListener("submit", this.preventSubmission);
    });

    // Disable all buttons except modal buttons
    const buttons = document.querySelectorAll(
      "button:not(#logoutNowBtn):not(#stayOnPageBtn)"
    );
    buttons.forEach((button) => {
      button.disabled = true;
    });
  }

  enablePageInteractions() {
    // Remove the class from body
    document.body.classList.remove("session-expired-active");

    // Re-enable form submissions
    const forms = document.querySelectorAll("form");
    forms.forEach((form) => {
      form.removeEventListener("submit", this.preventSubmission);
    });

    // Re-enable all buttons
    const buttons = document.querySelectorAll("button");
    buttons.forEach((button) => {
      button.disabled = false;
    });
  }

  preventSubmission(e) {
    e.preventDefault();
    e.stopPropagation();
    return false;
  }

  // Method to reset the modal state
  reset() {
    this.clearCountdown();
    this.timeLeft = this.originalTimeLeft;
    this.isModalVisible = false;
  }
}

// Initialize the session expired handler
const sessionExpiredHandler = new SessionExpiredHandler();

// CSS for disabled state
const style = document.createElement("style");
style.textContent = `
    body.session-expired-active {
        pointer-events: none;
        user-select: none;
    }
    
    body.session-expired-active .session-expired-modal {
        pointer-events: auto;
    }
    
    body.session-expired-active .session-expired-modal * {
        pointer-events: auto;
    }
`;
document.head.appendChild(style);
