// Site-wide JavaScript functionality
document.addEventListener("DOMContentLoaded", function () {
  // Add smooth scrolling for anchor links
  document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener("click", function (e) {
      e.preventDefault();
      document.querySelector(this.getAttribute("href")).scrollIntoView({
        behavior: "smooth",
      });
    });
  });

  // Add click handlers for interactive elements
  document.querySelectorAll(".card-hover").forEach((card) => {
    card.addEventListener("click", function () {
      // Add click functionality here
      console.log("Card clicked:", this);
    });
  });

  // Initialize tooltips and other interactive elements
  initializeInteractiveElements();
});

function initializeInteractiveElements() {
  // Add any additional interactive functionality here
  console.log("Interactive elements initialized");
}

// Utility functions for the dashboard
function formatCurrency(amount) {
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

function updateProgressBar(elementId, percentage) {
  const progressBar = document.getElementById(elementId);
  if (progressBar) {
    progressBar.style.width = percentage + "%";
  }
}
