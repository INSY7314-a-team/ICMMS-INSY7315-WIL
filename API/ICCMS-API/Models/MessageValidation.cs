using System.ComponentModel.DataAnnotations;

namespace ICCMS_API.Models
{
    public class MessageValidationRules
    {
        public const int MaxContentLength = 5000;
        public const int MaxSubjectLength = 200;
        public const int MinContentLength = 1;
        public const int MaxAttachmentsPerMessage = 10;
        public const int MaxMessageLengthPerHour = 50;
        public const int MaxMessageLengthPerDay = 200;
        public const int SpamDetectionThreshold = 5; // Similar messages in short time
        public const int SpamTimeWindowMinutes = 10;
    }

    public class MessageValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Info;
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical,
    }

    public class CreateMessageRequest
    {
        [Required(ErrorMessage = "Sender ID is required")]
        public string SenderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Receiver ID is required")]
        public string ReceiverId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project ID is required")]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(
            MessageValidationRules.MaxSubjectLength,
            ErrorMessage = "Subject cannot exceed 200 characters"
        )]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(
            MessageValidationRules.MaxContentLength,
            MinimumLength = MessageValidationRules.MinContentLength,
            ErrorMessage = "Content must be between 1 and 5000 characters"
        )]
        public string Content { get; set; } = string.Empty;

        public string? ThreadId { get; set; }
        public string? ParentMessageId { get; set; }
        public List<string> ThreadParticipants { get; set; } = new List<string>();
        public string MessageType { get; set; } = "direct";
    }

    // public class CreateThreadRequest
    // {
    //     [Required(ErrorMessage = "Project ID is required")]
    //     public string ProjectId { get; set; } = string.Empty;

    //     [Required(ErrorMessage = "Subject is required")]
    //     [StringLength(
    //         MessageValidationRules.MaxSubjectLength,
    //         ErrorMessage = "Subject cannot exceed 200 characters"
    //     )]
    //     public string Subject { get; set; } = string.Empty;

    //     [Required(ErrorMessage = "Content is required")]
    //     [StringLength(
    //         MessageValidationRules.MaxContentLength,
    //         MinimumLength = MessageValidationRules.MinContentLength,
    //         ErrorMessage = "Content must be between 1 and 5000 characters"
    //     )]
    //     public string Content { get; set; } = string.Empty;

    //     [Required(ErrorMessage = "At least one participant is required")]
    //     [MinLength(1, ErrorMessage = "At least one participant is required")]
    //     public List<string> Participants { get; set; } = new List<string>();

    //     public string ThreadType { get; set; } = "general";
    //     public List<string> Tags { get; set; } = new List<string>();
    // }

    // public class ReplyToMessageRequest
    // {
    //     [Required(ErrorMessage = "Parent message ID is required")]
    //     public string ParentMessageId { get; set; } = string.Empty;

    //     [Required(ErrorMessage = "Content is required")]
    //     [StringLength(
    //         MessageValidationRules.MaxContentLength,
    //         MinimumLength = MessageValidationRules.MinContentLength,
    //         ErrorMessage = "Content must be between 1 and 5000 characters"
    //     )]
    //     public string Content { get; set; } = string.Empty;

    //     public List<string> AdditionalRecipients { get; set; } = new List<string>();
    // }

    public class BroadcastMessageRequest
    {
        [Required(ErrorMessage = "Sender ID is required")]
        public string SenderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project ID is required")]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(
            MessageValidationRules.MaxSubjectLength,
            ErrorMessage = "Subject cannot exceed 200 characters"
        )]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(
            MessageValidationRules.MaxContentLength,
            MinimumLength = MessageValidationRules.MinContentLength,
            ErrorMessage = "Content must be between 1 and 5000 characters"
        )]
        public string Content { get; set; } = string.Empty;
    }

    public class QuoteApprovalRequest
    {
        [Required(ErrorMessage = "Quote ID is required")]
        public string QuoteId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action is required")]
        public string Action { get; set; } = string.Empty; // "approved", "rejected", "created"

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;
    }

    public class InvoicePaymentRequest
    {
        [Required(ErrorMessage = "Invoice ID is required")]
        public string InvoiceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Action is required")]
        public string Action { get; set; } = string.Empty; // "paid", "overdue", "created"

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;
    }

    public class ProjectUpdateRequest
    {
        [Required(ErrorMessage = "Project ID is required")]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Update type is required")]
        public string UpdateType { get; set; } = string.Empty; // "status_changed", "milestone_reached", "phase_completed"

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;
    }

    public class SystemAlertRequest
    {
        [Required(ErrorMessage = "Alert type is required")]
        public string AlertType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one recipient is required")]
        [MinLength(1, ErrorMessage = "At least one recipient is required")]
        public List<string> Recipients { get; set; } = new List<string>();
    }
}
