document.addEventListener("DOMContentLoaded", function () {
  const systemTab = document.getElementById("system-tab");
  const directTab = document.getElementById("direct-tab");
  const threadsList = document.getElementById("threadsList");
  const noThreadSelected = document.getElementById("noThreadSelected");
  const threadView = document.getElementById("threadView");
  const messageInputArea = document.getElementById("messageInputArea");
  const sendThreadMessageBtn = document.getElementById("sendThreadMessageBtn");
  const threadMessageInput = document.getElementById("threadMessageInput");
  const refreshButton = document.getElementById("refreshButton");
  const closeChatButton = document.getElementById("closeChatButton");
  const newMessageButton = document.getElementById("newMessageButton");
  const sendMessageBtnModal = document.getElementById("sendMessageBtnModal");

  let currentUserId = "";
  let currentThreadId = null;

  // --- Initialization ---
  function initialize() {
    currentUserId = window.currentUserId;
    if (!currentUserId) {
      console.error("Could not determine current user ID.");
      return;
    }
    setupEventListeners();
    loadThreads("system");
  }

  // --- Event Listeners ---
  function setupEventListeners() {
    systemTab.addEventListener("click", () => switchTab("system"));
    directTab.addEventListener("click", () => switchTab("direct"));
    sendThreadMessageBtn.addEventListener("click", sendMessage);
    threadMessageInput.addEventListener("keydown", (e) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
      }
    });
    refreshButton.addEventListener("click", refreshCurrentThread);
    closeChatButton.addEventListener("click", closeChat);
    sendMessageBtnModal.addEventListener("click", sendNewDirectMessage);
  }

  // --- Tab and Thread Management ---
  function switchTab(tabType) {
    systemTab.classList.toggle("active", tabType === "system");
    directTab.classList.toggle("active", tabType === "direct");
    newMessageButton.style.display = tabType === "direct" ? "block" : "none";
    closeChat();
    loadThreads(tabType);
  }

  function loadThreads(type) {
    threadsList.innerHTML = "<p>Loading...</p>";
    const url =
      type === "system"
        ? "/Messages/GetUserThreadsByType?messageType=workflow"
        : "/Messages/GetUserThreadsByType?messageType=direct";

    fetch(url)
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          renderThreads(data.threads, type);
        } else {
          threadsList.innerHTML = "<p>Error loading threads.</p>";
        }
      })
      .catch(() => (threadsList.innerHTML = "<p>Error loading threads.</p>"));
  }

  function renderThreads(threads, type) {
    if (!threads || threads.length === 0) {
      threadsList.innerHTML = `<p>No ${type} messages.</p>`;
      return;
    }

    threadsList.innerHTML = threads
      .map((thread) => {
        const senderPrefix = thread.lastMessageSenderName
          ? `<strong>${thread.lastMessageSenderName}:</strong> `
          : "";
        const preview = thread.lastMessagePreview || "No preview available.";

        return `
            <div class="thread-item ${
              thread.hasUnreadMessages ? "unread" : ""
            }" data-thread-id="${thread.threadId}" data-subject="${
          thread.subject
        }" data-type="${type}">
                <div class="thread-content">
                    <div class="thread-header">
                        <span class="thread-subject">${thread.subject}</span>
                        <span class="thread-time">${new Date(
                          thread.lastMessageAt
                        ).toLocaleDateString()}</span>
                    </div>
                    <div class="thread-preview">${senderPrefix}${preview}</div>
                </div>
            </div>
        `;
      })
      .join("");

    document.querySelectorAll(".thread-item").forEach((item) => {
      item.addEventListener("click", () => selectThread(item));
    });
  }

  function selectThread(threadElement) {
    currentThreadId = threadElement.dataset.threadId;
    const subject = threadElement.dataset.subject;
    const type = threadElement.dataset.type;

    document
      .querySelectorAll(".thread-item")
      .forEach((el) => el.classList.remove("active"));
    threadElement.classList.add("active");

    noThreadSelected.style.display = "none";
    threadView.style.display = "flex";
    threadView.style.flexDirection = "column";
    threadView.style.height = "100%";

    document.getElementById("selectedThreadSubject").textContent = subject;
    messageInputArea.style.display = type === "direct" ? "flex" : "none";

    loadMessages(currentThreadId);
    markThreadAsRead(currentThreadId);
  }

  // --- Message Handling ---
  function loadMessages(threadId) {
    const messagesContainer = document.getElementById("messagesContainer");
    messagesContainer.innerHTML = "<p>Loading messages...</p>";

    fetch(`/Messages/GetThreadMessages?threadId=${threadId}`)
      .then((response) => response.json())
      .then((data) => {
        if (data.success && data.messages) {
          renderMessages(data.messages);
        } else {
          messagesContainer.innerHTML = "<p>Could not load messages.</p>";
        }
      })
      .catch(
        () => (messagesContainer.innerHTML = "<p>Error loading messages.</p>")
      );
  }

  function renderMessages(messages) {
    const messagesContainer = document.getElementById("messagesContainer");
    if (!messages || messages.length === 0) {
      messagesContainer.innerHTML =
        "<p>No messages in this conversation yet.</p>";
      return;
    }

    messagesContainer.innerHTML = messages
      .map((msg) => {
        const isMine = msg.senderId === currentUserId;
        const isSystem = msg.senderId === "system";
        let messageClass = "user-message-left";
        if (isSystem) messageClass = "system-message";
        if (isMine) messageClass = "user-message-right";

        // Render system messages differently
        if (isSystem) {
          return `
                <div class="message-item ${messageClass}">
                    <div class="message-content">
                        <div class="message-text">${msg.content}</div>
                        <div class="message-time"> - System at ${new Date(
                          msg.sentAt
                        ).toLocaleTimeString()}</div>
                    </div>
                </div>
            `;
        }

        // Render regular user messages
        const senderName = msg.senderName || "Unknown User";
        return `
                <div class="message-item ${messageClass}">
                    <div class="message-content">
                        ${
                          !isMine && !isSystem
                            ? `<div class="message-sender">${senderName}</div>`
                            : ""
                        }
                        <div class="message-text">${msg.content}
                         <span class="message-time-chip">${new Date(
                           msg.sentAt
                         ).toLocaleTimeString()}</span></div>
                        
                    </div>
                </div>
            `;
      })
      .join("");

    messagesContainer.scrollTop = messagesContainer.scrollHeight;
  }

  function sendMessage() {
    const content = threadMessageInput.value.trim();
    if (!content || !currentThreadId) return;

    fetch("/Messages/SendThreadMessage", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ threadId: currentThreadId, content: content }),
    })
      .then((res) => res.json())
      .then((data) => {
        if (data.success) {
          threadMessageInput.value = "";
          loadMessages(currentThreadId);
        } else {
          alert("Failed to send message.");
        }
      })
      .catch(() => alert("Error sending message."));
  }

  function sendNewDirectMessage() {
    const recipientId = document.getElementById("recipientSelect").value;
    const projectId = document.getElementById("projectSelect").value;
    const subject = document.getElementById("messageSubject").value;
    const content = document.getElementById("messageContent").value;

    if (!recipientId || !subject || !content) {
      alert("Please fill out all required fields.");
      return;
    }

    fetch("/Messages/SendMessage", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        receiverId: recipientId,
        projectId: projectId,
        subject: subject,
        content: content,
      }),
    })
      .then((res) => res.json())
      .then((data) => {
        if (data.success) {
          const modal = bootstrap.Modal.getInstance(
            document.getElementById("sendMessageModal")
          );
          modal.hide();
          document.getElementById("sendMessageForm").reset();
          // Refresh direct messages to show the new thread
          loadThreads("direct");
        } else {
          alert("Failed to send message: " + data.message);
        }
      })
      .catch((error) => {
        console.error("Error sending new message:", error);
        alert("An error occurred while sending the message.");
      });
  }

  function markThreadAsRead(threadId) {
    fetch("/Messages/MarkThreadAsRead", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ threadId: threadId }),
    })
      .then((res) => res.json())
      .then((data) => {
        if (data.success) {
          const threadItem = document.querySelector(
            `.thread-item[data-thread-id="${threadId}"]`
          );
          if (threadItem) threadItem.classList.remove("unread");
          // Since this might affect the total count, let's refresh the threads
          // This will ensure the data is consistent
          const activeTab = document
            .querySelector(".tab-button.active")
            .id.replace("-tab", "");
          loadThreads(activeTab);
        }
      });
  }

  function refreshCurrentThread() {
    if (currentThreadId) {
      loadMessages(currentThreadId);
    }
  }

  function closeChat() {
    currentThreadId = null;
    document
      .querySelectorAll(".thread-item")
      .forEach((el) => el.classList.remove("active"));
    threadView.style.display = "none";
    noThreadSelected.style.display = "flex";
  }

  // --- Run ---
  initialize();
});
