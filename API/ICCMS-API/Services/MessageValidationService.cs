using System.Text.RegularExpressions;
using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class MessageValidationService : IMessageValidationService
    {
        private readonly IFirebaseService _firebaseService;
        private readonly List<string> _spamKeywords;
        private readonly Dictionary<string, List<DateTime>> _userMessageHistory;

        public MessageValidationService(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            _spamKeywords = new List<string>
            {
                "click here",
                "free money",
                "urgent",
                "act now",
                "limited time",
                "guaranteed",
                "no risk",
                "make money",
                "work from home",
                "get rich",
                "instant",
                "miracle",
                "secret",
                "exclusive",
            };
            _userMessageHistory = new Dictionary<string, List<DateTime>>();
        }

        public async Task<MessageValidationResult> ValidateMessageAsync(
            CreateMessageRequest request
        )
        {
            var result = new MessageValidationResult { IsValid = true };

            // If ThreadId is provided, we're replying to an existing thread
            // In this case, some fields will be populated from the thread data
            bool isThreadReply = !string.IsNullOrEmpty(request.ThreadId);

            // Basic field validation
            if (string.IsNullOrWhiteSpace(request.SenderId))
                result.Errors.Add("Sender ID is required");

            // For thread replies, ReceiverId, ProjectId, and Subject will be populated from thread
            if (!isThreadReply)
            {
                if (string.IsNullOrWhiteSpace(request.ReceiverId))
                    result.Errors.Add("Receiver ID is required");

                if (string.IsNullOrWhiteSpace(request.ProjectId))
                    result.Errors.Add("Project ID is required");

                if (string.IsNullOrWhiteSpace(request.Subject))
                    result.Errors.Add("Subject is required");
            }

            // Subject length validation (only if subject is provided)
            if (
                !string.IsNullOrWhiteSpace(request.Subject)
                && request.Subject.Length > MessageValidationRules.MaxSubjectLength
            )
                result.Errors.Add(
                    $"Subject cannot exceed {MessageValidationRules.MaxSubjectLength} characters"
                );

            if (string.IsNullOrWhiteSpace(request.Content))
                result.Errors.Add("Content is required");
            else if (request.Content.Length < MessageValidationRules.MinContentLength)
                result.Errors.Add(
                    $"Content must be at least {MessageValidationRules.MinContentLength} character(s)"
                );
            else if (request.Content.Length > MessageValidationRules.MaxContentLength)
                result.Errors.Add(
                    $"Content cannot exceed {MessageValidationRules.MaxContentLength} characters"
                );

            // Validate users exist
            if (!string.IsNullOrEmpty(request.SenderId))
            {
                var sender = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    request.SenderId
                );
                if (sender == null)
                    result.Errors.Add("Sender not found");
            }

            // Only validate receiver if it's provided (not a thread reply)
            if (!isThreadReply && !string.IsNullOrEmpty(request.ReceiverId))
            {
                var receiver = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    request.ReceiverId
                );
                if (receiver == null)
                    result.Errors.Add("Receiver not found");
            }

            // Spam detection (only if we have a ProjectId)
            if (
                !string.IsNullOrEmpty(request.ProjectId)
                && await IsSpamAsync(request.SenderId, request.Content, request.ProjectId)
            )
            {
                result.Errors.Add("Message appears to be spam");
                result.Severity = ValidationSeverity.Critical;
            }

            // Rate limiting
            if (await IsRateLimitedAsync(request.SenderId))
            {
                result.Errors.Add(
                    "Rate limit exceeded. Please wait before sending another message."
                );
                result.Severity = ValidationSeverity.Warning;
            }

            // Content quality checks
            if (await ContainsSpamKeywordsAsync(request.Content))
            {
                result.Warnings.Add("Message contains potentially suspicious content");
                result.Severity = ValidationSeverity.Warning;
            }

            // Check for excessive capitalization
            if (IsExcessiveCapitalization(request.Content))
            {
                result.Warnings.Add("Message contains excessive capitalization");
            }

            // Check for excessive punctuation
            if (IsExcessivePunctuation(request.Content))
            {
                result.Warnings.Add("Message contains excessive punctuation");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<MessageValidationResult> ValidateThreadAsync(CreateThreadRequest request)
        {
            var result = new MessageValidationResult { IsValid = true };

            // Basic field validation
            if (string.IsNullOrWhiteSpace(request.ProjectId))
                result.Errors.Add("Project ID is required");

            if (string.IsNullOrWhiteSpace(request.Subject))
                result.Errors.Add("Subject is required");
            else if (request.Subject.Length > MessageValidationRules.MaxSubjectLength)
                result.Errors.Add(
                    $"Subject cannot exceed {MessageValidationRules.MaxSubjectLength} characters"
                );

            if (string.IsNullOrWhiteSpace(request.Content))
                result.Errors.Add("Content is required");
            else if (request.Content.Length < MessageValidationRules.MinContentLength)
                result.Errors.Add(
                    $"Content must be at least {MessageValidationRules.MinContentLength} character(s)"
                );
            else if (request.Content.Length > MessageValidationRules.MaxContentLength)
                result.Errors.Add(
                    $"Content cannot exceed {MessageValidationRules.MaxContentLength} characters"
                );

            if (request.Participants == null || !request.Participants.Any())
                result.Errors.Add("At least one participant is required");

            // Validate participants exist
            if (request.Participants != null)
            {
                foreach (var participantId in request.Participants)
                {
                    var user = await _firebaseService.GetDocumentAsync<User>(
                        "users",
                        participantId
                    );
                    if (user == null)
                        result.Errors.Add($"Participant {participantId} not found");
                }
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<MessageValidationResult> ValidateReplyAsync(ReplyToMessageRequest request)
        {
            var result = new MessageValidationResult { IsValid = true };

            // Basic field validation
            if (string.IsNullOrWhiteSpace(request.ParentMessageId))
                result.Errors.Add("Parent message ID is required");

            if (string.IsNullOrWhiteSpace(request.Content))
                result.Errors.Add("Content is required");
            else if (request.Content.Length < MessageValidationRules.MinContentLength)
                result.Errors.Add(
                    $"Content must be at least {MessageValidationRules.MinContentLength} character(s)"
                );
            else if (request.Content.Length > MessageValidationRules.MaxContentLength)
                result.Errors.Add(
                    $"Content cannot exceed {MessageValidationRules.MaxContentLength} characters"
                );

            // Validate parent message exists
            if (!string.IsNullOrEmpty(request.ParentMessageId))
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var parentMessage = messages.FirstOrDefault(m =>
                    m.MessageId == request.ParentMessageId
                );
                if (parentMessage == null)
                    result.Errors.Add("Parent message not found");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<MessageValidationResult> ValidateBroadcastAsync(
            BroadcastMessageRequest request
        )
        {
            var result = new MessageValidationResult { IsValid = true };

            // Basic field validation
            if (string.IsNullOrWhiteSpace(request.SenderId))
                result.Errors.Add("Sender ID is required");

            if (string.IsNullOrWhiteSpace(request.ProjectId))
                result.Errors.Add("Project ID is required");

            if (string.IsNullOrWhiteSpace(request.Subject))
                result.Errors.Add("Subject is required");
            else if (request.Subject.Length > MessageValidationRules.MaxSubjectLength)
                result.Errors.Add(
                    $"Subject cannot exceed {MessageValidationRules.MaxSubjectLength} characters"
                );

            if (string.IsNullOrWhiteSpace(request.Content))
                result.Errors.Add("Content is required");
            else if (request.Content.Length < MessageValidationRules.MinContentLength)
                result.Errors.Add(
                    $"Content must be at least {MessageValidationRules.MinContentLength} character(s)"
                );
            else if (request.Content.Length > MessageValidationRules.MaxContentLength)
                result.Errors.Add(
                    $"Content cannot exceed {MessageValidationRules.MaxContentLength} characters"
                );

            // Validate sender exists
            if (!string.IsNullOrEmpty(request.SenderId))
            {
                var sender = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    request.SenderId
                );
                if (sender == null)
                    result.Errors.Add("Sender not found");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<bool> IsSpamAsync(string senderId, string content, string projectId)
        {
            try
            {
                // Check for similar messages in the last 10 minutes
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var recentMessages = messages
                    .Where(m =>
                        m.SenderId == senderId
                        && m.ProjectId == projectId
                        && m.SentAt
                            > DateTime.UtcNow.AddMinutes(
                                -MessageValidationRules.SpamTimeWindowMinutes
                            )
                    )
                    .ToList();

                // Check for duplicate content
                var duplicateCount = recentMessages.Count(m =>
                    m.Content.Equals(content, StringComparison.OrdinalIgnoreCase)
                );

                if (duplicateCount >= MessageValidationRules.SpamDetectionThreshold)
                    return true;

                // Check for very similar content (simple similarity check)
                var similarCount = recentMessages.Count(m =>
                    CalculateSimilarity(m.Content, content) > 0.8
                );

                if (similarCount >= MessageValidationRules.SpamDetectionThreshold)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking spam: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsRateLimitedAsync(string senderId)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Initialize user history if not exists
                if (!_userMessageHistory.ContainsKey(senderId))
                {
                    _userMessageHistory[senderId] = new List<DateTime>();
                }

                var userHistory = _userMessageHistory[senderId];

                // Clean old entries
                userHistory.RemoveAll(t => t < now.AddHours(-24));

                // Check hourly limit
                var hourlyMessages = userHistory.Count(t => t > now.AddHours(-1));
                if (hourlyMessages >= MessageValidationRules.MaxMessageLengthPerHour)
                    return true;

                // Check daily limit
                var dailyMessages = userHistory.Count(t => t > now.AddDays(-1));
                if (dailyMessages >= MessageValidationRules.MaxMessageLengthPerDay)
                    return true;

                // Add current message timestamp
                userHistory.Add(now);

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking rate limit: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetSpamKeywordsAsync()
        {
            return _spamKeywords;
        }

        public async Task<bool> ContainsSpamKeywordsAsync(string content)
        {
            var lowerContent = content.ToLower();
            return _spamKeywords.Any(keyword => lowerContent.Contains(keyword));
        }

        private bool IsExcessiveCapitalization(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            var upperCount = content.Count(char.IsUpper);
            var totalCount = content.Count(char.IsLetter);

            if (totalCount == 0)
                return false;

            var upperPercentage = (double)upperCount / totalCount;
            return upperPercentage > 0.7; // More than 70% uppercase
        }

        private bool IsExcessivePunctuation(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            var punctuationCount = content.Count(char.IsPunctuation);
            var totalCount = content.Length;

            if (totalCount == 0)
                return false;

            var punctuationPercentage = (double)punctuationCount / totalCount;
            return punctuationPercentage > 0.3; // More than 30% punctuation
        }

        private double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }
    }
}
