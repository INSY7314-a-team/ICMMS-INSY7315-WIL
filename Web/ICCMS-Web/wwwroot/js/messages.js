// Messages JavaScript functionality
class MessagesManager {
  constructor() {
    this.currentThreadId = null;
    this.refreshInterval = null;
    this.init();
  }

  init() {
    this.setupEventListeners();
    this.startAutoRefresh();
    this.updateUnreadCount();
  }

  setupEventListeners() {
    // Search functionality
    const searchInput = document.getElementById("searchThreads");
    if (searchInput) {
      searchInput.addEventListener(
        "input",
        this.debounce(this.searchThreads.bind(this), 300)
      );
    }

    // Thread selection
    document.addEventListener("click", (e) => {
      if (e.target.closest(".thread-item")) {
        const threadItem = e.target.closest(".thread-item");
        const threadId = threadItem.dataset.threadId;
        const subject = threadItem.querySelector(".thread-subject").textContent;
        this.selectThread(threadId, subject);
      }
    });

    // Message form submission
    const sendButton = document.querySelector('[onclick="sendMessage()"]');
    if (sendButton) {
      sendButton.addEventListener("click", this.sendMessage.bind(this));
    }

    // Modal events
    const sendModal = document.getElementById("sendMessageModal");
    if (sendModal) {
      sendModal.addEventListener(
        "hidden.bs.modal",
        this.resetSendForm.bind(this)
      );
    }

    // Message type filter events
    const filterButtons = document.querySelectorAll(
      'input[name="messageTypeFilter"]'
    );
    filterButtons.forEach((button) => {
      button.addEventListener("change", this.filterMessagesByType.bind(this));
    });
  }

  selectThread(threadId, subject) {
    this.currentThreadId = threadId;

    // Update UI
    this.updateThreadSelection(threadId);
    this.showThreadView(subject);
    this.loadThreadMessages(threadId);
  }

  updateThreadSelection(threadId) {
    // Remove active class from all threads
    document.querySelectorAll(".thread-item").forEach((item) => {
      item.classList.remove("active");
    });

    // Add active class to selected thread
    const selectedThread = document.querySelector(
      `[data-thread-id="${threadId}"]`
    );
    if (selectedThread) {
      selectedThread.classList.add("active");
    }
  }

  showThreadView(subject) {
    document.getElementById("noThreadSelected").style.display = "none";
    document.getElementById("threadView").style.display = "block";
    document.getElementById("selectedThreadSubject").textContent = subject;
  }

  async loadThreadMessages(threadId) {
    const container = document.getElementById("messagesContainer");
    container.innerHTML = this.createLoadingHTML();

    try {
      const response = await fetch(
        `/Messages/GetThreadMessages?threadId=${threadId}`
      );
      const data = await response.json();

      if (data.success) {
        this.displayMessages(data.messages);
        this.markThreadAsRead(threadId);
      } else {
        container.innerHTML = this.createErrorHTML("Failed to load messages");
      }
    } catch (error) {
      console.error("Error loading messages:", error);
      container.innerHTML = this.createErrorHTML("Error loading messages");
    }
  }

  displayMessages(messages) {
    const container = document.getElementById("messagesContainer");

    if (!messages || messages.length === 0) {
      container.innerHTML = this.createEmptyStateHTML();
      return;
    }

    let html = "";
    messages.forEach((message) => {
      html += this.createMessageHTML(message);
    });

    container.innerHTML = html;
    this.scrollToBottom(container);
  }

  createMessageHTML(message) {
    const isSystemMessage = message.senderId === "system";
    const messageClass = isSystemMessage ? "system-message" : "user-message";
    const senderName = isSystemMessage
      ? "System"
      : message.senderName || "Unknown User";
    const avatarIcon = isSystemMessage ? "robot" : "user";
    const sentTime = new Date(message.sentAt).toLocaleString();

    return `
            <div class="message-item ${messageClass}">
                <div class="message-avatar">
                    <i class="fas fa-${avatarIcon}"></i>
                </div>
                <div class="message-content">
                    <div class="message-header">
                        <span class="message-sender">${senderName}</span>
                        <span class="message-time">${sentTime}</span>
                    </div>
                    <div class="message-text">${this.escapeHtml(
                      message.content
                    )}</div>
                    ${
                      isSystemMessage
                        ? '<div class="system-message-indicator">System Message</div>'
                        : ""
                    }
                </div>
            </div>
        `;
  }

  async sendMessage() {
    const recipientId = document.getElementById("recipientSelect").value;
    const projectId = document.getElementById("projectSelect").value;
    const subject = document.getElementById("messageSubject").value;
    const content = document.getElementById("messageContent").value;

    if (!this.validateSendForm(recipientId, subject, content)) {
      return;
    }

    const request = {
      receiverId: recipientId,
      projectId: projectId,
      subject: subject,
      content: content,
    };

    try {
      const response = await fetch("/Messages/SendMessage", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify(request),
      });

      const data = await response.json();

      if (data.success) {
        this.closeSendModal();
        this.resetSendForm();
        this.showAlert("success", data.message);
        this.refreshThreadsList();
      } else {
        this.showAlert("danger", data.message);
      }
    } catch (error) {
      console.error("Error sending message:", error);
      this.showAlert("danger", "Error sending message");
    }
  }

  validateSendForm(recipientId, subject, content) {
    if (!recipientId) {
      this.showAlert("warning", "Please select a recipient");
      return false;
    }
    if (!subject.trim()) {
      this.showAlert("warning", "Please enter a subject");
      return false;
    }
    if (!content.trim()) {
      this.showAlert("warning", "Please enter a message");
      return false;
    }
    return true;
  }

  async updateUnreadCount() {
    try {
      const response = await fetch("/Messages/GetUnreadCount");
      const data = await response.json();

      if (data.success) {
        this.updateUnreadBadge(data.count);
      }
    } catch (error) {
      console.error("Error updating unread count:", error);
    }
  }

  async filterMessagesByType() {
    const selectedFilter = document.querySelector(
      'input[name="messageTypeFilter"]:checked'
    ).value;

    try {
      // Fetch filtered threads from server
      const response = await fetch(
        `/Messages/GetUserThreadsByType?messageType=${selectedFilter}`
      );
      const data = await response.json();

      if (data.success) {
        this.updateThreadsList(data.threads);
      } else {
        console.error("Error filtering threads:", data.message);
        this.showAlert("error", "Error filtering messages");
      }
    } catch (error) {
      console.error("Error filtering messages:", error);
      this.showAlert("error", "Error filtering messages");
    }
  }

  updateThreadsList(threads) {
    const threadsList = document.getElementById("threadsList");
    if (!threadsList) return;

    if (threads.length === 0) {
      threadsList.innerHTML = `
        <div class="no-messages" id="noMessages">
          <div class="no-messages-icon">
            <i class="fas fa-comments"></i>
          </div>
          <h5>No messages found</h5>
          <p>No messages match your current filter.</p>
        </div>
      `;
      return;
    }

    // Clear existing threads
    threadsList.innerHTML = "";

    // Add filtered threads
    threads.forEach((thread) => {
      const threadItem = this.createThreadItem(thread);
      threadsList.appendChild(threadItem);
    });
  }

  createThreadItem(thread) {
    const threadItem = document.createElement("div");
    threadItem.className = `thread-item ${
      thread.hasUnreadMessages ? "unread" : ""
    }`;
    threadItem.setAttribute("data-thread-id", thread.threadId);
    threadItem.setAttribute("data-message-type", thread.threadType || "direct");
    threadItem.onclick = () =>
      this.selectThread(thread.threadId, thread.subject);

    const avatarIcon =
      thread.threadType === "workflow" ? "fas fa-cogs" : "fas fa-comments";
    const preview =
      thread.lastMessagePreview && thread.lastMessagePreview.length > 50
        ? thread.lastMessagePreview.substring(0, 47) + "..."
        : thread.lastMessagePreview || "";

    threadItem.innerHTML = `
      <div class="thread-avatar">
        <i class="${avatarIcon}"></i>
      </div>
      <div class="thread-content">
        <div class="thread-header">
          <h6 class="thread-subject">${thread.subject}</h6>
          <span class="thread-time">${new Date(
            thread.lastMessageAt
          ).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
          })}</span>
        </div>
        <div class="thread-preview">
          <span class="thread-sender">${thread.lastMessageSenderName}:</span>
          <span class="thread-message">${preview}</span>
        </div>
        ${
          thread.hasUnreadMessages ? '<div class="unread-indicator"></div>' : ""
        }
      </div>
    `;

    return threadItem;
  }

  updateNoMessagesState() {
    const visibleThreads = document.querySelectorAll(
      '.thread-item[style*="block"], .thread-item:not([style*="none"])'
    );
    const noMessagesDiv = document.getElementById("noMessages");

    if (visibleThreads.length === 0 && noMessagesDiv) {
      noMessagesDiv.style.display = "block";
    } else if (noMessagesDiv) {
      noMessagesDiv.style.display = "none";
    }
  }

  updateUnreadBadge(count) {
    const badge = document.getElementById("unreadCount");
    const badgeContainer = document.getElementById("unreadBadge");

    if (count > 0) {
      badge.textContent = count;
      badgeContainer.style.display = "inline-block";
    } else {
      badgeContainer.style.display = "none";
    }
  }

  async refreshThreadsList() {
    try {
      const response = await fetch("/Messages/GetUserThreads");
      const data = await response.json();

      if (data.success) {
        this.updateThreadsList(data.threads);
      }
    } catch (error) {
      console.error("Error refreshing threads:", error);
    }
  }

  updateThreadsList(threads) {
    const container = document.getElementById("threadsList");

    if (!threads || threads.length === 0) {
      container.innerHTML = this.createEmptyThreadsHTML();
      return;
    }

    let html = "";
    threads.forEach((thread) => {
      html += this.createThreadHTML(thread);
    });

    container.innerHTML = html;
  }

  createThreadHTML(thread) {
    const hasUnread = thread.hasUnreadMessages ? "unread" : "";
    const unreadBadge = thread.hasUnreadMessages
      ? `<span class="thread-unread-badge">${thread.unreadCount}</span>`
      : "";

    const preview =
      thread.lastMessagePreview.length > 50
        ? thread.lastMessagePreview.substring(0, 47) + "..."
        : thread.lastMessagePreview;

    const time = new Date(thread.lastMessageAt).toLocaleDateString();

    return `
            <div class="thread-item ${hasUnread}" data-thread-id="${
      thread.threadId
    }">
                <div class="thread-avatar">
                    <i class="fas fa-comments"></i>
                </div>
                <div class="thread-content">
                    <div class="thread-header">
                        <h6 class="thread-subject">${this.escapeHtml(
                          thread.subject
                        )}</h6>
                        <span class="thread-time">${time}</span>
                    </div>
                    <div class="thread-preview">
                        <span class="thread-sender">${this.escapeHtml(
                          thread.lastMessageSenderName
                        )}:</span>
                        <span class="thread-message">${this.escapeHtml(
                          preview
                        )}</span>
                    </div>
                    <div class="thread-meta">
                        <span class="thread-project">${this.escapeHtml(
                          thread.projectName
                        )}</span>
                        ${unreadBadge}
                    </div>
                </div>
            </div>
        `;
  }

  searchThreads(event) {
    const searchTerm = event.target.value.toLowerCase();
    const threads = document.querySelectorAll(".thread-item");

    threads.forEach((thread) => {
      const subject = thread
        .querySelector(".thread-subject")
        .textContent.toLowerCase();
      const sender = thread
        .querySelector(".thread-sender")
        .textContent.toLowerCase();
      const message = thread
        .querySelector(".thread-message")
        .textContent.toLowerCase();
      const project = thread
        .querySelector(".thread-project")
        .textContent.toLowerCase();

      const matches =
        subject.includes(searchTerm) ||
        sender.includes(searchTerm) ||
        message.includes(searchTerm) ||
        project.includes(searchTerm);

      thread.style.display = matches ? "flex" : "none";
    });
  }

  markThreadAsRead(threadId) {
    // This would typically mark all messages in the thread as read
    // For now, we'll just update the UI
    const threadItem = document.querySelector(`[data-thread-id="${threadId}"]`);
    if (threadItem) {
      threadItem.classList.remove("unread");
      const unreadBadge = threadItem.querySelector(".thread-unread-badge");
      if (unreadBadge) {
        unreadBadge.remove();
      }
    }
  }

  startAutoRefresh() {
    // Refresh unread count every 30 seconds
    this.refreshInterval = setInterval(() => {
      this.updateUnreadCount();
    }, 30000);
  }

  stopAutoRefresh() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  // Utility methods
  createLoadingHTML() {
    return `
            <div class="text-center py-4">
                <div class="spinner-border" role="status"></div>
                <p class="mt-2">Loading messages...</p>
            </div>
        `;
  }

  createErrorHTML(message) {
    return `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>${message}
            </div>
        `;
  }

  createEmptyStateHTML() {
    return `
            <div class="empty-state">
                <i class="fas fa-comment-slash"></i>
                <p>No messages in this conversation</p>
            </div>
        `;
  }

  createEmptyThreadsHTML() {
    return `
            <div class="empty-state">
                <i class="fas fa-comment-slash"></i>
                <p>No conversations yet</p>
                <small>Start a conversation by sending a message</small>
            </div>
        `;
  }

  closeSendModal() {
    const modal = bootstrap.Modal.getInstance(
      document.getElementById("sendMessageModal")
    );
    if (modal) {
      modal.hide();
    }
  }

  resetSendForm() {
    document.getElementById("sendMessageForm").reset();
  }

  scrollToBottom(element) {
    element.scrollTop = element.scrollHeight;
  }

  showAlert(type, message) {
    const alertDiv = document.createElement("div");
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.innerHTML = `
            <i class="fas fa-${
              type === "success" ? "check-circle" : "exclamation-triangle"
            } me-2"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

    const container = document.querySelector(".messages-container");
    const content = document.querySelector(".messages-content");
    container.insertBefore(alertDiv, content);

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
      if (alertDiv.parentNode) {
        alertDiv.remove();
      }
    }, 5000);
  }

  getAntiForgeryToken() {
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    return token ? token.value : "";
  }

  escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }
}

// Initialize messages manager when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  window.messagesManager = new MessagesManager();
});

// Global functions for backward compatibility
function selectThread(threadId, subject) {
  if (window.messagesManager) {
    window.messagesManager.selectThread(threadId, subject);
  }
}

function loadThreadMessages(threadId) {
  if (window.messagesManager) {
    window.messagesManager.loadThreadMessages(threadId);
  }
}

function sendMessage() {
  if (window.messagesManager) {
    window.messagesManager.sendMessage();
  }
}

function updateUnreadCount() {
  if (window.messagesManager) {
    window.messagesManager.updateUnreadCount();
  }
}

function refreshThread() {
  if (window.messagesManager && window.messagesManager.currentThreadId) {
    window.messagesManager.loadThreadMessages(
      window.messagesManager.currentThreadId
    );
  }
}

function showAlert(type, message) {
  if (window.messagesManager) {
    window.messagesManager.showAlert(type, message);
  }
}
