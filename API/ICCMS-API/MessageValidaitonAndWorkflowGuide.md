# Message Validation and Workflow Integration Guide

This document describes the message validation system and workflow integration that has been added to the ICCMS messaging system.

## Overview

The messaging system now includes:

- **Message Validation**: Content length limits, required field validation, and spam protection
- **Workflow Integration**: Automatic message generation for system events
- **System Notifications**: Integration with quote/invoice approval workflows
- **Rate Limiting**: Protection against message flooding
- **Content Quality Checks**: Detection of suspicious or low-quality content

## Message Validation

### Validation Rules

The system enforces the following validation rules:

```csharp
public class MessageValidationRules
{
    public const int MaxContentLength = 5000;
    public const int MaxSubjectLength = 200;
    public const int MinContentLength = 1;
    public const int MaxAttachmentsPerMessage = 10;
    public const int MaxMessageLengthPerHour = 50;
    public const int MaxMessageLengthPerDay = 200;
    public const int SpamDetectionThreshold = 5;
    public const int SpamTimeWindowMinutes = 10;
}
```

### Validation Features

#### 1. Content Length Validation

- **Minimum Content**: 1 character
- **Maximum Content**: 5,000 characters
- **Maximum Subject**: 200 characters
- **Maximum Attachments**: 10 per message

#### 2. Rate Limiting

- **Hourly Limit**: 50 messages per user per hour
- **Daily Limit**: 200 messages per user per day
- **Automatic Cleanup**: Old message history is automatically cleaned

#### 3. Spam Protection

- **Duplicate Detection**: Prevents sending identical messages
- **Similarity Detection**: Detects very similar messages (80%+ similarity)
- **Time Window**: Checks messages within the last 10 minutes
- **Threshold**: Blocks if 5+ similar messages detected

#### 4. Content Quality Checks

- **Spam Keywords**: Detects common spam phrases
- **Excessive Capitalization**: Flags messages with >70% uppercase
- **Excessive Punctuation**: Flags messages with >30% punctuation
- **User Validation**: Ensures sender and receiver exist

### Validation Response

The validation system returns detailed feedback:

```json
{
  "isValid": false,
  "errors": [
    "Content cannot exceed 5000 characters",
    "Rate limit exceeded. Please wait before sending another message."
  ],
  "warnings": [
    "Message contains potentially suspicious content",
    "Message contains excessive capitalization"
  ],
  "severity": "Warning"
}
```

## Workflow Integration

### System Events

The workflow system automatically generates messages for important system events:

#### 1. Quote Workflow Events

- **Quote Created**: Notifies relevant stakeholders
- **Quote Approved**: Confirms approval to contractor
- **Quote Rejected**: Notifies contractor of rejection with feedback

#### 2. Invoice Workflow Events

- **Invoice Generated**: Notifies client of new invoice
- **Invoice Paid**: Confirms payment to contractor
- **Invoice Overdue**: Alerts client of overdue payment

#### 3. Project Workflow Events

- **Status Changed**: Notifies team of project status updates
- **Milestone Reached**: Celebrates project milestones
- **Phase Completed**: Notifies of phase completion

#### 4. System Events

- **Maintenance Alerts**: System maintenance notifications
- **Security Alerts**: Security-related notifications
- **System Updates**: Feature updates and announcements

### Message Templates

The system uses configurable templates for workflow messages:

```csharp
public class WorkflowMessageTemplate
{
    public string WorkflowType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string ContentTemplate { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public List<string> DefaultRecipients { get; set; } = new List<string>();
    public Dictionary<string, string> Placeholders { get; set; } = new Dictionary<string, string>();
}
```

### Template Examples

#### Quote Approval Template

```csharp
{
    "WorkflowType": "quotation",
    "Action": "approved",
    "SubjectTemplate": "Quotation Approved - {quoteId}",
    "ContentTemplate": "Quotation {quoteId} has been approved. Amount: {quoteTotal:C}. You can proceed with the work.",
    "Priority": "high"
}
```

## API Endpoints

### Message Validation Endpoints

#### 1. Create Message (with validation)

**Endpoint:** `POST /api/messages`

**Request Body:**

```json
{
  "senderId": "user123",
  "receiverId": "user456",
  "projectId": "project789",
  "subject": "Project Update",
  "content": "The project is progressing well. We've completed phase 1.",
  "threadId": "thread123",
  "parentMessageId": null,
  "threadParticipants": ["user123", "user456"],
  "messageType": "direct"
}
```

**Response (Success):**

```json
{
  "messageId": "msg-uuid-123",
  "warnings": []
}
```

**Response (Validation Error):**

```json
{
  "errors": [
    "Content cannot exceed 5000 characters",
    "Rate limit exceeded. Please wait before sending another message."
  ],
  "warnings": ["Message contains potentially suspicious content"],
  "severity": "Warning"
}
```

### Workflow Integration Endpoints

#### 1. Get Workflow Messages

**Endpoint:** `GET /api/messages/workflow`

**Query Parameters:**

- `projectId` (optional): Filter by project
- `workflowType` (optional): Filter by workflow type

#### 2. Send Quote Approval Notification

**Endpoint:** `POST /api/messages/workflow/quote-approval`

#### 3. Send Invoice Payment Notification

**Endpoint:** `POST /api/messages/workflow/invoice-payment`

#### 4. Send Project Update Notification

**Endpoint:** `POST /api/messages/workflow/project-update`

#### 5. Send System Alert

**Endpoint:** `POST /api/messages/workflow/system-alert`

## Usage Examples

### JavaScript/TypeScript Integration

```javascript
// Create a message with validation
const createMessage = async (messageData) => {
  try {
    const response = await fetch("/api/messages", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify(messageData),
    });

    const result = await response.json();

    if (response.ok) {
      console.log("Message created:", result.messageId);
      if (result.warnings && result.warnings.length > 0) {
        console.warn("Warnings:", result.warnings);
      }
    } else {
      console.error("Validation errors:", result.errors);
      console.warn("Warnings:", result.warnings);
    }
  } catch (error) {
    console.error("Error creating message:", error);
  }
};
```

## Configuration

### Validation Rules Configuration

You can customize validation rules by modifying the `MessageValidationRules` class:

```csharp
public class MessageValidationRules
{
    public const int MaxContentLength = 5000;        // Maximum message content length
    public const int MaxSubjectLength = 200;         // Maximum subject length
    public const int MinContentLength = 1;           // Minimum content length
    public const int MaxAttachmentsPerMessage = 10;  // Maximum attachments per message
    public const int MaxMessageLengthPerHour = 50;   // Rate limiting: messages per hour
    public const int MaxMessageLengthPerDay = 200;   // Rate limiting: messages per day
    public const int SpamDetectionThreshold = 5;     // Spam detection threshold
    public const int SpamTimeWindowMinutes = 10;     // Spam detection time window
}
```

## Security Considerations

### Message Validation Security

1. **Input Sanitization**: All message content is validated and sanitized
2. **Rate Limiting**: Prevents message flooding and abuse
3. **Spam Detection**: Multiple layers of spam protection
4. **User Validation**: Ensures only valid users can send messages
5. **Content Filtering**: Detects and flags suspicious content

### Workflow Security

1. **Authentication**: All workflow endpoints require authentication
2. **Authorization**: Users can only trigger workflows they have permission for
3. **Audit Trail**: All workflow messages are logged and tracked
4. **Template Validation**: Message templates are validated before use
5. **Recipient Validation**: Only valid users receive workflow messages

## Error Handling

### Validation Errors

The system provides detailed error messages for validation failures:

```json
{
  "errors": [
    "Content cannot exceed 5000 characters",
    "Subject is required",
    "Rate limit exceeded. Please wait before sending another message."
  ],
  "warnings": [
    "Message contains potentially suspicious content",
    "Message contains excessive capitalization"
  ],
  "severity": "Warning"
}
```

## Performance Considerations

### Validation Performance

1. **Caching**: User validation results are cached
2. **Async Operations**: All validation operations are asynchronous
3. **Efficient Queries**: Database queries are optimized
4. **Memory Management**: Message history is automatically cleaned up

### Workflow Performance

1. **Batch Processing**: Multiple recipients are processed efficiently
2. **Template Caching**: Message templates are cached in memory
3. **Async Notifications**: Push notifications are sent asynchronously
4. **Error Recovery**: Failed workflow messages are retried automatically

## Best Practices

### Message Validation

1. **Client-Side Validation**: Implement client-side validation for better UX
2. **Progressive Enhancement**: Show warnings without blocking message sending
3. **User Education**: Inform users about validation rules
4. **Regular Updates**: Keep spam keywords and rules updated

### Workflow Integration

1. **Event-Driven**: Use workflow messages for important system events
2. **Template Management**: Regularly review and update message templates
3. **Recipient Management**: Ensure accurate recipient lists
4. **Testing**: Test workflow messages in development environment

## Troubleshooting

### Common Validation Issues

1. **Rate Limiting**: Users hitting rate limits should wait before sending more messages
2. **Spam Detection**: False positives can be resolved by adjusting spam keywords
3. **Content Length**: Users should be informed about content length limits
4. **User Validation**: Ensure user IDs are correct and users exist

### Common Workflow Issues

1. **Template Not Found**: Ensure templates are properly configured
2. **Recipient Issues**: Verify recipient user IDs are valid
3. **Notification Failures**: Check device tokens and notification service status
4. **Permission Errors**: Ensure users have appropriate permissions

## Future Enhancements

### Planned Features

1. **Advanced Spam Detection**: Machine learning-based spam detection
2. **Custom Validation Rules**: User-configurable validation rules
3. **Message Templates UI**: Web interface for managing message templates
4. **Analytics Dashboard**: Comprehensive analytics for message and workflow metrics
5. **Multi-language Support**: Support for multiple languages in templates
6. **Advanced Rate Limiting**: More sophisticated rate limiting algorithms
7. **Message Encryption**: End-to-end encryption for sensitive messages
8. **Workflow Automation**: Visual workflow builder for custom automation
