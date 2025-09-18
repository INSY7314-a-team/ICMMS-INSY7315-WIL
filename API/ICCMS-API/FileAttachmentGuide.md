# File Attachment Support for Messaging

This document describes the file attachment functionality that has been added to the ICCMS messaging system.

## Overview

The messaging system now supports:

- **File Attachments**: Attach files to messages and thread replies
- **Multiple File Types**: Support for images, documents, and other file types
- **File Storage**: Integration with Supabase storage for secure file handling
- **Attachment Management**: Upload, view, and delete attachments
- **Push Notifications**: Notifications include attachment indicators
- **File Categorization**: Organize attachments by category and type

## Data Models

### Enhanced Message Model

The `Message` model has been extended with attachment support:

```csharp
public class Message
{
    // Existing fields...

    // File attachment fields
    public List<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public bool HasAttachments { get; set; } = false;
}
```

### MessageAttachment Model

```csharp
public class MessageAttachment
{
    public string AttachmentId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsImage { get; set; } = false;
    public bool IsDocument { get; set; } = false;
    public string Category { get; set; } = "general";
    public string Status { get; set; } = "active";
}
```

## API Endpoints

### 1. Upload File Attachment

**Endpoint:** `POST /api/messages/attachment`

**Content-Type:** `multipart/form-data`

**Form Data:**

- `file`: The file to upload (required)
- `messageId`: ID of the message to attach to (required)
- `description`: Optional description of the attachment
- `category`: Optional category (default: "general")

**Response:**

```json
{
  "attachmentId": "attachment-uuid",
  "fileName": "generated-filename.ext",
  "originalFileName": "original-filename.ext",
  "fileType": "image/jpeg",
  "fileSize": 1024000,
  "fileUrl": "https://supabase-url/storage/v1/object/public/message-attachments/filename.ext",
  "thumbnailUrl": null,
  "uploadedAt": "2024-01-15T10:30:00Z",
  "description": "Attachment description",
  "isImage": true,
  "isDocument": false,
  "category": "general"
}
```

### 2. Get Attachment Details

**Endpoint:** `GET /api/messages/attachment/{attachmentId}`

**Response:**

```json
{
  "attachmentId": "attachment-uuid",
  "fileName": "generated-filename.ext",
  "originalFileName": "original-filename.ext",
  "fileType": "application/pdf",
  "fileSize": 2048000,
  "fileUrl": "https://supabase-url/storage/v1/object/public/message-attachments/filename.ext",
  "thumbnailUrl": null,
  "uploadedAt": "2024-01-15T10:30:00Z",
  "description": "Project blueprint",
  "isImage": false,
  "isDocument": true,
  "category": "blueprint"
}
```

### 3. Get Message Attachments

**Endpoint:** `GET /api/messages/message/{messageId}/attachments`

**Response:**

```json
[
  {
    "attachmentId": "attachment-uuid-1",
    "fileName": "image1.jpg",
    "originalFileName": "project-photo.jpg",
    "fileType": "image/jpeg",
    "fileSize": 1024000,
    "fileUrl": "https://supabase-url/storage/v1/object/public/message-attachments/image1.jpg",
    "thumbnailUrl": null,
    "uploadedAt": "2024-01-15T10:30:00Z",
    "description": "Progress photo",
    "isImage": true,
    "isDocument": false,
    "category": "photo"
  },
  {
    "attachmentId": "attachment-uuid-2",
    "fileName": "document1.pdf",
    "originalFileName": "contract.pdf",
    "fileType": "application/pdf",
    "fileSize": 2048000,
    "fileUrl": "https://supabase-url/storage/v1/object/public/message-attachments/document1.pdf",
    "thumbnailUrl": null,
    "uploadedAt": "2024-01-15T10:35:00Z",
    "description": "Contract document",
    "isImage": false,
    "isDocument": true,
    "category": "contract"
  }
]
```

### 4. Delete Attachment

**Endpoint:** `DELETE /api/messages/attachment/{attachmentId}`

**Response:** `204 No Content`

## Supported File Types

### Images

- JPEG/JPG
- PNG
- GIF
- BMP
- WebP
- SVG

### Documents

- PDF
- Microsoft Word (.doc, .docx)
- Microsoft Excel (.xls, .xlsx)
- Microsoft PowerPoint (.ppt, .pptx)
- Plain Text (.txt)
- CSV
- RTF

### File Size Limits

- Maximum file size: 50MB per attachment
- No limit on number of attachments per message

## Attachment Categories

The system supports the following attachment categories:

- **general**: General purpose attachments
- **blueprint**: Architectural or engineering blueprints
- **photo**: Project photos and images
- **document**: Official documents and contracts
- **contract**: Legal contracts and agreements
- **invoice**: Invoice and billing documents
- **quotation**: Quotation and estimate documents

## Usage Examples

### Uploading an Attachment

```javascript
// Upload a file attachment to a message
const uploadAttachment = async (
  messageId,
  file,
  description = "",
  category = "general"
) => {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("messageId", messageId);
  formData.append("description", description);
  formData.append("category", category);

  const response = await fetch("/api/messages/attachment", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });

  if (response.ok) {
    const attachment = await response.json();
    console.log("Attachment uploaded:", attachment);
    return attachment;
  } else {
    throw new Error("Failed to upload attachment");
  }
};
```

### Getting Message Attachments

```javascript
// Get all attachments for a message
const getMessageAttachments = async (messageId) => {
  const response = await fetch(
    `/api/messages/message/${messageId}/attachments`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (response.ok) {
    const attachments = await response.json();
    console.log("Message attachments:", attachments);
    return attachments;
  } else {
    throw new Error("Failed to get attachments");
  }
};
```

### Deleting an Attachment

```javascript
// Delete an attachment
const deleteAttachment = async (attachmentId) => {
  const response = await fetch(`/api/messages/attachment/${attachmentId}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (response.ok) {
    console.log("Attachment deleted successfully");
  } else {
    throw new Error("Failed to delete attachment");
  }
};
```

## Mobile App Integration

### File Upload Component

```javascript
// React Native file upload component
import { launchImageLibrary, launchCamera } from "react-native-image-picker";
import DocumentPicker from "react-native-document-picker";

const AttachmentUploader = ({ messageId, onAttachmentUploaded }) => {
  const uploadFile = async (file) => {
    try {
      const formData = new FormData();
      formData.append("file", {
        uri: file.uri,
        type: file.type,
        name: file.fileName || file.name,
      });
      formData.append("messageId", messageId);
      formData.append("description", file.description || "");
      formData.append("category", file.category || "general");

      const response = await fetch("/api/messages/attachment", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "multipart/form-data",
        },
        body: formData,
      });

      if (response.ok) {
        const attachment = await response.json();
        onAttachmentUploaded(attachment);
      }
    } catch (error) {
      console.error("Upload failed:", error);
    }
  };

  const selectImage = () => {
    launchImageLibrary({ mediaType: "photo" }, (response) => {
      if (response.assets && response.assets[0]) {
        uploadFile(response.assets[0]);
      }
    });
  };

  const selectDocument = async () => {
    try {
      const result = await DocumentPicker.pick({
        type: [
          DocumentPicker.types.pdf,
          DocumentPicker.types.doc,
          DocumentPicker.types.docx,
        ],
      });
      uploadFile(result[0]);
    } catch (error) {
      if (!DocumentPicker.isCancel(error)) {
        console.error("Document picker error:", error);
      }
    }
  };

  return (
    <View style={styles.container}>
      <TouchableOpacity onPress={selectImage} style={styles.button}>
        <Text>📷 Add Photo</Text>
      </TouchableOpacity>
      <TouchableOpacity onPress={selectDocument} style={styles.button}>
        <Text>📄 Add Document</Text>
      </TouchableOpacity>
    </View>
  );
};
```

### Attachment Display Component

```javascript
// Display attachments in a message
const AttachmentList = ({ attachments }) => {
  const renderAttachment = (attachment) => {
    if (attachment.isImage) {
      return (
        <TouchableOpacity
          key={attachment.attachmentId}
          onPress={() => openImage(attachment.fileUrl)}
        >
          <Image
            source={{ uri: attachment.fileUrl }}
            style={styles.attachmentImage}
          />
          <Text style={styles.attachmentName}>
            {attachment.originalFileName}
          </Text>
        </TouchableOpacity>
      );
    } else {
      return (
        <TouchableOpacity
          key={attachment.attachmentId}
          onPress={() => openDocument(attachment.fileUrl)}
        >
          <View style={styles.documentAttachment}>
            <Text style={styles.documentIcon}>📄</Text>
            <Text style={styles.attachmentName}>
              {attachment.originalFileName}
            </Text>
            <Text style={styles.fileSize}>
              {formatFileSize(attachment.fileSize)}
            </Text>
          </View>
        </TouchableOpacity>
      );
    }
  };

  return (
    <View style={styles.attachmentsContainer}>
      {attachments.map(renderAttachment)}
    </View>
  );
};
```

## Push Notifications

Push notifications now include attachment indicators:

- **With Attachments**: "New message from John: Project update 📎 (2 attachments)"
- **Without Attachments**: "New message from John: Project update"

The notification data includes:

- `hasAttachments`: Boolean indicating if message has attachments
- `attachmentCount`: Number of attachments (if any)

## Security Considerations

### File Validation

- File type validation based on MIME type
- File size limits (50MB maximum)
- Unique filename generation to prevent conflicts

### Access Control

- Only authenticated users can upload attachments
- Users can only delete their own attachments
- Attachment access is controlled by message permissions

### Storage Security

- Files stored in Supabase with proper access controls
- Unique filenames prevent unauthorized access
- Soft delete for attachments (status change, not physical deletion)

## Error Handling

The system handles various error scenarios:

- **File Too Large**: Returns 400 with size limit message
- **Invalid File Type**: Returns 400 with supported types message
- **Message Not Found**: Returns 404 when attaching to non-existent message
- **Upload Failure**: Returns 500 with error details
- **Storage Errors**: Graceful handling of Supabase storage issues

## Performance Considerations

### File Upload

- Streaming upload for large files
- Progress tracking for upload status
- Background upload processing

### File Access

- Direct URL access for file downloads
- Caching for frequently accessed files
- Thumbnail generation for images (future enhancement)

### Storage Management

- Automatic cleanup of orphaned files
- Storage quota monitoring
- File compression for large documents

## Best Practices

### File Organization

- Use appropriate categories for better organization
- Provide meaningful descriptions for attachments
- Keep file names descriptive and organized

### User Experience

- Show upload progress for large files
- Provide preview for images and documents
- Allow batch upload for multiple files

### Security

- Validate file types on both client and server
- Implement virus scanning for uploaded files
- Regular security audits of stored files

## Future Enhancements

### Planned Features

- **Thumbnail Generation**: Automatic thumbnail creation for images
- **File Preview**: In-app preview for documents and images
- **Batch Upload**: Upload multiple files at once
- **File Versioning**: Track file changes and versions
- **Advanced Search**: Search attachments by content and metadata
- **File Sharing**: Share attachments outside of messages
- **Cloud Integration**: Direct integration with cloud storage services
