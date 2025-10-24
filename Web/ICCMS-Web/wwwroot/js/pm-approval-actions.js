// PM Approval Actions JavaScript
document.addEventListener("DOMContentLoaded", function () {
  console.log("🎯 PM Approval Actions Initialized");

  initializeApprovalActions();

  console.log("✅ PM Approval Actions components initialized");
});

function initializeApprovalActions() {
  // Progress reports are now auto-approved, no action buttons needed

  // Task completion approval buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".approve-completion-btn")) {
      const button = e.target.closest(".approve-completion-btn");
      const taskId = button.getAttribute("data-task-id");
      approveTaskCompletion(taskId);
    }
  });

  // Task completion rejection buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".reject-completion-btn")) {
      const button = e.target.closest(".reject-completion-btn");
      const taskId = button.getAttribute("data-task-id");
      showTaskCompletion(taskId);
    }
  });

  // Task completion view details buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-completion-details-btn")) {
      const button = e.target.closest(".view-completion-details-btn");
      const taskId = button.getAttribute("data-task-id");
      showTaskCompletion(taskId);
    }
  });

  // Attachment viewing buttons
  document.addEventListener("click", function (e) {
    if (e.target.closest(".view-attachment-btn")) {
      const button = e.target.closest(".view-attachment-btn");
      const documentId = button.getAttribute("data-document-id");
      const reportType = button.getAttribute("data-report-type");
      const reportId = button.getAttribute("data-report-id");
      viewAttachment(documentId, reportType, reportId);
    }
  });
}

// Progress report approval functions removed - reports are now auto-approved

function approveTaskCompletion(taskId) {
  console.log("Approve task completion:", taskId);

  if (!confirm("Are you sure you want to approve this task completion?")) {
    return;
  }

  // Show loading state
  showInfoMessage("Approving task completion...");

  // Make real API call
  fetch(
    `/ProjectManager/ApproveCompletionReport?id=${encodeURIComponent(taskId)}`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
    }
  )
    .then(async (response) => {
      console.log("📡 Approval response status:", response.status);
      const responseData = await response.json();

      if (!response.ok) {
        throw new Error(
          responseData.error || `HTTP error! status: ${response.status}`
        );
      }

      return responseData;
    })
    .then((data) => {
      console.log("✅ Task completion approved successfully:", data);

      if (data.success) {
        showSuccessMessage("Task completion approved successfully");

        // Remove the approval item from the UI
        const approvalItem = document
          .querySelector(`[data-task-id="${taskId}"]`)
          ?.closest(".approval-item");
        if (approvalItem) {
          approvalItem.remove();
        }

        // Refresh approvals list
        if (typeof window.refreshApprovalsList === "function") {
          window.refreshApprovalsList();
        }
      } else {
        throw new Error(data.error || "Unknown error occurred");
      }
    })
    .catch((error) => {
      console.error("❌ Failed to approve task completion:", error);
      showErrorMessage("Failed to approve task completion: " + error.message);
    });
}

function showTaskCompletion(taskId) {
  console.log("Show task completion:", taskId);

  // Show the task completion review modal
  if (typeof window.showTaskCompletion === "function") {
    window.showTaskCompletion(taskId);
  } else {
    showErrorMessage("Task completion review modal not available");
  }
}

function viewAttachment(documentId, reportType, reportId) {
  console.log("View attachment:", documentId, reportType, reportId);

  // Extract filename from Supabase URL if it's a full URL
  let filename = documentId;
  if (documentId.includes("supabase.co/storage/v1/object/public/upload/")) {
    filename = documentId.split("/upload/")[1];
    console.log("Extracted filename from URL:", filename);
  } else if (documentId.includes("/upload/")) {
    // Handle other possible URL formats
    filename = documentId.split("/upload/")[1];
    console.log("Extracted filename from alternative URL format:", filename);
  } else {
    console.log("Using documentId as filename:", filename);
  }

  // Show the document viewer modal
  const modal = new bootstrap.Modal(
    document.getElementById("documentViewerModal")
  );
  const content = document.getElementById("documentViewerContent");
  const downloadBtn = document.getElementById("downloadDocumentBtn");

  // Show loading state
  content.innerHTML = `
    <div class="text-center py-4">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <p class="mt-2">Loading document...</p>
    </div>
  `;

  // Set up download button
  downloadBtn.onclick = () => {
    const documentUrl = `https://localhost:7136/api/documents/${encodeURIComponent(
      filename
    )}`;
    window.open(documentUrl, "_blank");
  };

  // Try to load the document
  const documentUrl = `https://localhost:7136/api/documents/${encodeURIComponent(
    filename
  )}`;

  console.log("Attempting to fetch document from URL:", documentUrl);
  console.log("Filename being used:", filename);

  fetch(documentUrl)
    .then((response) => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      // Check if response is JSON (Base64 encoded) or binary
      const contentType = response.headers.get("content-type");
      console.log("Response content type:", contentType);

      if (contentType && contentType.includes("application/json")) {
        // Handle JSON response (Base64 encoded data)
        console.log("Detected JSON response, parsing Base64 data...");
        return response.json().then((data) => {
          console.log("JSON data received, length:", data ? data.length : 0);
          // Convert Base64 to blob
          const binaryString = atob(data);
          const bytes = new Uint8Array(binaryString.length);
          for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
          }
          console.log("Converted to binary, size:", bytes.length);
          return new Blob([bytes], { type: "application/octet-stream" });
        });
      } else {
        // Handle binary response
        console.log("Detected binary response, getting blob...");
        return response.blob().then((blob) => {
          console.log(
            "Binary blob received, size:",
            blob.size,
            "type:",
            blob.type
          );
          return blob;
        });
      }
    })
    .then((blob) => {
      const url = URL.createObjectURL(blob);

      // Try to detect file type from filename if blob type is generic
      let fileType = blob.type;
      if (fileType === "application/octet-stream" || !fileType) {
        const extension = filename.split(".").pop().toLowerCase();
        const mimeTypes = {
          jpg: "image/jpeg",
          jpeg: "image/jpeg",
          png: "image/png",
          gif: "image/gif",
          webp: "image/webp",
          svg: "image/svg+xml",
          pdf: "application/pdf",
          txt: "text/plain",
          csv: "text/csv",
          html: "text/html",
          htm: "text/html",
          json: "application/json",
          xml: "text/xml",
          docx: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
          doc: "application/msword",
          xlsx: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          xls: "application/vnd.ms-excel",
          pptx: "application/vnd.openxmlformats-officedocument.presentationml.presentation",
          ppt: "application/vnd.ms-powerpoint",
        };

        if (mimeTypes[extension]) {
          fileType = mimeTypes[extension];
          console.log("Detected file type from extension:", fileType);
          // Create a new blob with the correct MIME type
          blob = new Blob([blob], { type: fileType });
        }
      }

      console.log("File type detected:", fileType);

      if (fileType.startsWith("image/")) {
        // Display image
        content.innerHTML = `
          <div class="text-center">
            <img src="${url}" class="img-fluid" style="max-height: 70vh;" alt="Document">
          </div>
        `;
      } else if (fileType === "application/pdf") {
        // Display PDF
        content.innerHTML = `
          <div class="text-center">
            <iframe src="${url}" width="100%" height="600px" style="border: none;"></iframe>
          </div>
        `;
      } else if (fileType === "text/plain" || fileType === "text/csv") {
        // Display text files
        fetch(url)
          .then((response) => response.text())
          .then((text) => {
            content.innerHTML = `
              <div class="text-start">
                <pre class="bg-light p-3 rounded" style="max-height: 70vh; overflow-y: auto; white-space: pre-wrap;">${text}</pre>
              </div>
            `;
          })
          .catch(() => {
            content.innerHTML = `
              <div class="text-center py-4">
                <i class="fa-solid fa-file-text fa-3x mb-3"></i>
                <p>Text file detected but could not be loaded.</p>
                <p>Click the Download button to view the document.</p>
              </div>
            `;
          });
      } else if (fileType.includes("html") || fileType === "text/html") {
        // Display HTML files
        content.innerHTML = `
          <div class="text-center">
            <iframe src="${url}" width="100%" height="600px" style="border: 1px solid #ddd;"></iframe>
          </div>
        `;
      } else if (fileType.includes("json") || fileType === "application/json") {
        // Display JSON files
        fetch(url)
          .then((response) => response.json())
          .then((json) => {
            content.innerHTML = `
              <div class="text-start">
                <pre class="bg-light p-3 rounded" style="max-height: 70vh; overflow-y: auto;">${JSON.stringify(
                  json,
                  null,
                  2
                )}</pre>
              </div>
            `;
          })
          .catch(() => {
            content.innerHTML = `
              <div class="text-center py-4">
                <i class="fa-solid fa-file-code fa-3x mb-3"></i>
                <p>JSON file detected but could not be parsed.</p>
                <p>Click the Download button to view the document.</p>
              </div>
            `;
          });
      } else if (
        fileType.includes("xml") ||
        fileType === "text/xml" ||
        fileType === "application/xml"
      ) {
        // Display XML files
        fetch(url)
          .then((response) => response.text())
          .then((text) => {
            content.innerHTML = `
              <div class="text-start">
                <pre class="bg-light p-3 rounded" style="max-height: 70vh; overflow-y: auto; white-space: pre-wrap;">${text}</pre>
              </div>
            `;
          })
          .catch(() => {
            content.innerHTML = `
              <div class="text-center py-4">
                <i class="fa-solid fa-file-code fa-3x mb-3"></i>
                <p>XML file detected but could not be loaded.</p>
                <p>Click the Download button to view the document.</p>
              </div>
            `;
          });
      } else if (
        fileType.includes("word") ||
        fileType.includes("document") ||
        fileType ===
          "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
        fileType === "application/msword"
      ) {
        // DOCX/DOC files - show info and download option
        content.innerHTML = `
          <div class="text-center py-4">
            <i class="fa-solid fa-file-word fa-3x mb-3 text-primary"></i>
            <h5>Microsoft Word Document</h5>
            <p>Word documents cannot be previewed in the browser.</p>
            <p>Click the Download button to open the document in Microsoft Word or another compatible application.</p>
            <div class="mt-3">
              <button class="btn btn-primary" onclick="window.open('${documentUrl}', '_blank')">
                <i class="fa-solid fa-download me-1"></i>Download Document
              </button>
            </div>
          </div>
        `;
      } else if (
        fileType.includes("excel") ||
        fileType.includes("spreadsheet") ||
        fileType ===
          "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
        fileType === "application/vnd.ms-excel"
      ) {
        // XLSX/XLS files - show info and download option
        content.innerHTML = `
          <div class="text-center py-4">
            <i class="fa-solid fa-file-excel fa-3x mb-3 text-success"></i>
            <h5>Microsoft Excel Spreadsheet</h5>
            <p>Excel files cannot be previewed in the browser.</p>
            <p>Click the Download button to open the spreadsheet in Microsoft Excel or another compatible application.</p>
            <div class="mt-3">
              <button class="btn btn-success" onclick="window.open('${documentUrl}', '_blank')">
                <i class="fa-solid fa-download me-1"></i>Download Spreadsheet
              </button>
            </div>
          </div>
        `;
      } else if (
        fileType.includes("powerpoint") ||
        fileType.includes("presentation") ||
        fileType ===
          "application/vnd.openxmlformats-officedocument.presentationml.presentation" ||
        fileType === "application/vnd.ms-powerpoint"
      ) {
        // PPTX/PPT files - show info and download option
        content.innerHTML = `
          <div class="text-center py-4">
            <i class="fa-solid fa-file-powerpoint fa-3x mb-3 text-warning"></i>
            <h5>Microsoft PowerPoint Presentation</h5>
            <p>PowerPoint files cannot be previewed in the browser.</p>
            <p>Click the Download button to open the presentation in Microsoft PowerPoint or another compatible application.</p>
            <div class="mt-3">
              <button class="btn btn-warning" onclick="window.open('${documentUrl}', '_blank')">
                <i class="fa-solid fa-download me-1"></i>Download Presentation
              </button>
            </div>
          </div>
        `;
      } else {
        // For other file types, show download option with generic icon
        const fileIcon = getFileIcon(fileType);
        content.innerHTML = `
          <div class="text-center py-4">
            <i class="fa-solid ${fileIcon} fa-3x mb-3"></i>
            <h5>Document Preview Not Available</h5>
            <p>This document type (${fileType}) cannot be previewed in the browser.</p>
            <p>Click the Download button to view the document with an appropriate application.</p>
            <div class="mt-3">
              <button class="btn btn-primary" onclick="window.open('${documentUrl}', '_blank')">
                <i class="fa-solid fa-download me-1"></i>Download Document
              </button>
            </div>
          </div>
        `;
      }
    })
    .catch((error) => {
      console.error("Error loading document:", error);
      content.innerHTML = `
        <div class="text-center py-4">
          <i class="fa-solid fa-exclamation-triangle fa-3x mb-3 text-warning"></i>
          <p>Failed to load document</p>
          <p class="text-muted">${error.message}</p>
          <button class="btn btn-primary" onclick="window.open('${documentUrl}', '_blank')">
            <i class="fa-solid fa-external-link-alt me-1"></i>Open in New Tab
          </button>
        </div>
      `;
    });

  modal.show();
}

// Helper function to get appropriate file icon based on MIME type
function getFileIcon(fileType) {
  if (fileType.startsWith("image/")) return "fa-file-image";
  if (fileType === "application/pdf") return "fa-file-pdf";
  if (fileType.includes("word") || fileType.includes("document"))
    return "fa-file-word";
  if (fileType.includes("excel") || fileType.includes("spreadsheet"))
    return "fa-file-excel";
  if (fileType.includes("powerpoint") || fileType.includes("presentation"))
    return "fa-file-powerpoint";
  if (fileType.includes("text/") || fileType === "text/plain")
    return "fa-file-text";
  if (fileType.includes("json") || fileType === "application/json")
    return "fa-file-code";
  if (fileType.includes("xml")) return "fa-file-code";
  if (fileType.includes("html")) return "fa-file-code";
  if (fileType.includes("zip") || fileType.includes("archive"))
    return "fa-file-archive";
  if (fileType.includes("video/")) return "fa-file-video";
  if (fileType.includes("audio/")) return "fa-file-audio";
  return "fa-file"; // Default file icon
}

// Global functions for use by other scripts
window.approveProgressReport = approveProgressReport;
window.showProgressReport = showProgressReport;
window.approveTaskCompletion = approveTaskCompletion;
window.showTaskCompletion = showTaskCompletion;

// Utility functions
function showSuccessMessage(message) {
  if (typeof window.PMProjectDetail?.showSuccessMessage === "function") {
    window.PMProjectDetail.showSuccessMessage(message);
  } else {
    console.log("Success:", message);
  }
}

function showErrorMessage(message) {
  if (typeof window.PMProjectDetail?.showErrorMessage === "function") {
    window.PMProjectDetail.showErrorMessage(message);
  } else {
    console.error("Error:", message);
  }
}

function showInfoMessage(message) {
  if (typeof window.PMProjectDetail?.showInfoMessage === "function") {
    window.PMProjectDetail.showInfoMessage(message);
  } else {
    console.log("Info:", message);
  }
}
