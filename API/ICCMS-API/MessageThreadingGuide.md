# Message Threading Implementation Guide

This document describes the message threading functionality that has been added to the ICCMS messaging system.

## Overview

The messaging system now supports:

- **Conversation Threading**: Group related messages into threads
- **Reply Functionality**: Reply to specific messages within threads
- **Thread Management**: Create, view, and manage conversation threads
- **Project-based Threading**: Organize threads by project or topic
- **Push Notifications**: Thread-aware notifications for new replies

## Data Models

### Updated Message Model

The `Message` model has been enhanced with threading fields:

```csharp
public class Message
{
    // Existing fields...

    // Threading fields
    public string ThreadId { get; set; } = string.Empty;
    public string? ParentMessageId { get; set; }
    public bool IsThreadStarter { get; set; } = false;
    public int ThreadDepth { get; set; } = 0;
    public int ReplyCount { get; set; } = 0;
    public DateTime? LastReplyAt { get; set; }
    public List<string> ThreadParticipants { get; set; } = new List<string>();
    public string MessageType { get; set; } = "direct"; // "direct", "thread", "broadcast"
}
```

### New MessageThread Model

```csharp
public class MessageThread
{
    public string ThreadId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string StarterMessageId { get; set; } = string.Empty;
    public string StarterUserId { get; set; } = string.Empty;
    public List<string> Participants { get; set; } = new List<string>();
    public int MessageCount { get; set; } = 0;
    public DateTime LastMessageAt { get; set; }
    public string LastMessageId { get; set; } = string.Empty;
    public string LastMessageSenderId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string ThreadType { get; set; } = "general"; // "general", "support", "project_update", "quotation", "invoice"
    public List<string> Tags { get; set; } = new List<string>();
}
```

## API Endpoints

### 1. Create a New Thread

**Endpoint:** `POST /api/messages/thread`

**Request Body:**

```json
{
  "projectId": "project123",
  "subject": "Thread Subject",
  "content": "Initial message content",
  "participants": ["user1", "user2", "user3"],
  "threadType": "general",
  "tags": ["urgent", "project-update"]
}
```

**Response:**

```json
{
  "threadId": "thread-uuid",
  "messageId": "message-uuid"
}
```

### 2. Reply to a Message

**Endpoint:** `POST /api/messages/reply`

**Request Body:**

```json
{
  "parentMessageId": "parent-message-id",
  "content": "Reply content",
  "additionalRecipients": ["user4", "user5"]
}
```

**Response:**

```json
"reply-message-id"
```

### 3. Get All Threads

**Endpoint:** `GET /api/messages/threads?projectId=project123`

**Response:**

```json
[
  {
    "threadId": "thread-uuid",
    "subject": "Thread Subject",
    "projectId": "project123",
    "messageCount": 5,
    "lastMessageAt": "2024-01-15T10:30:00Z",
    "lastMessageSenderName": "John Doe",
    "lastMessagePreview": "This is the last message content...",
    "participants": ["user1", "user2", "user3"],
    "threadType": "general",
    "hasUnreadMessages": true,
    "unreadCount": 2
  }
]
```

### 4. Get Thread Messages

**Endpoint:** `GET /api/messages/thread/{threadId}`

**Response:**

```json
[
  {
    "messageId": "message-uuid",
    "senderId": "user1",
    "receiverId": "user2",
    "projectId": "project123",
    "subject": "Thread Subject",
    "content": "Message content",
    "sentAt": "2024-01-15T10:00:00Z",
    "threadId": "thread-uuid",
    "parentMessageId": null,
    "isThreadStarter": true,
    "threadDepth": 0,
    "messageType": "thread"
  },
  {
    "messageId": "reply-uuid",
    "senderId": "user2",
    "receiverId": "user1",
    "projectId": "project123",
    "subject": "Re: Thread Subject",
    "content": "Reply content",
    "sentAt": "2024-01-15T10:15:00Z",
    "threadId": "thread-uuid",
    "parentMessageId": "message-uuid",
    "isThreadStarter": false,
    "threadDepth": 1,
    "messageType": "thread"
  }
]
```

### 5. Create Regular Message (Enhanced)

**Endpoint:** `POST /api/messages`

The existing message creation endpoint now automatically handles threading:

- If `ThreadId` is provided, the message is added to that thread
- If `ThreadId` is empty, a new thread is created
- Thread metadata is automatically updated

## Thread Types

The system supports different thread types for better organization:

- **general**: General project discussions
- **support**: Support requests and issues
- **project_update**: Project status updates
- **quotation**: Quotation-related discussions
- **invoice**: Invoice-related discussions

## Threading Features

### 1. Automatic Thread Management

- Thread metadata is automatically updated when messages are added
- Message counts and last message timestamps are maintained
- Participant lists are automatically updated

### 2. Reply Chain Tracking

- Each reply tracks its parent message
- Thread depth is calculated automatically
- Reply chains can be followed back to the original message

### 3. Unread Message Tracking

- Unread message counts per thread
- Per-user unread tracking
- Thread-level read status

### 4. Push Notifications

- Thread-aware notifications
- Different notification types for thread replies vs. new threads
- Rich notification data including thread context

## Usage Examples

### Creating a Project Discussion Thread

```javascript
// Create a new thread for project discussion
const createThread = async () => {
  const response = await fetch("/api/messages/thread", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      projectId: "project123",
      subject: "Weekly Project Update",
      content: "Let's discuss the progress made this week...",
      participants: ["pm1", "contractor1", "client1"],
      threadType: "project_update",
      tags: ["weekly", "progress"],
    }),
  });

  const result = await response.json();
  console.log("Thread created:", result.threadId);
};
```

### Replying to a Message

```javascript
// Reply to a specific message in a thread
const replyToMessage = async (parentMessageId) => {
  const response = await fetch("/api/messages/reply", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      parentMessageId: parentMessageId,
      content: "Thanks for the update. I have a few questions...",
      additionalRecipients: ["supervisor1"],
    }),
  });

  const messageId = await response.text();
  console.log("Reply sent:", messageId);
};
```

### Getting Thread List

```javascript
// Get all threads for a project
const getProjectThreads = async (projectId) => {
  const response = await fetch(`/api/messages/threads?projectId=${projectId}`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  const threads = await response.json();
  console.log("Project threads:", threads);
};
```

## Mobile App Integration

### Thread List View

```javascript
// Display threads in a list view
const ThreadList = ({ projectId }) => {
  const [threads, setThreads] = useState([]);

  useEffect(() => {
    fetchThreads(projectId).then(setThreads);
  }, [projectId]);

  return (
    <FlatList
      data={threads}
      keyExtractor={(item) => item.threadId}
      renderItem={({ item }) => (
        <ThreadItem
          thread={item}
          onPress={() => navigateToThread(item.threadId)}
        />
      )}
    />
  );
};
```

### Thread View

```javascript
// Display messages in a thread
const ThreadView = ({ threadId }) => {
  const [messages, setMessages] = useState([]);

  useEffect(() => {
    fetchThreadMessages(threadId).then(setMessages);
  }, [threadId]);

  return (
    <FlatList
      data={messages}
      keyExtractor={(item) => item.messageId}
      renderItem={({ item }) => (
        <MessageItem
          message={item}
          onReply={() => replyToMessage(item.messageId)}
        />
      )}
    />
  );
};
```

## Best Practices

### 1. Thread Organization

- Use descriptive thread subjects
- Choose appropriate thread types
- Add relevant tags for better categorization

### 2. Participant Management

- Include all relevant stakeholders in thread participants
- Add new participants when needed
- Remove inactive participants

### 3. Message Content

- Keep messages focused on the thread topic
- Use clear, concise language
- Reference previous messages when needed

### 4. Performance Considerations

- Thread lists are paginated for large projects
- Message history is loaded on-demand
- Unread counts are cached for performance

## Error Handling

The system gracefully handles various error scenarios:

- **Invalid Thread ID**: Returns 404 for non-existent threads
- **Missing Parent Message**: Returns 404 for invalid reply attempts
- **Permission Errors**: Returns 403 for unauthorized access
- **Database Errors**: Returns 500 with error details

## Security Considerations

- All endpoints require proper authentication
- Users can only access threads they participate in
- Thread participants are validated before adding messages
- Sensitive information is not exposed in thread summaries
