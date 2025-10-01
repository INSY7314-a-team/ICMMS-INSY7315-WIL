using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IMessageValidationService
    {
        Task<MessageValidationResult> ValidateMessageAsync(CreateMessageRequest request);
        Task<MessageValidationResult> ValidateThreadAsync(CreateThreadRequest request);
        Task<MessageValidationResult> ValidateReplyAsync(ReplyToMessageRequest request);
        Task<MessageValidationResult> ValidateBroadcastAsync(BroadcastMessageRequest request);
        Task<bool> IsSpamAsync(string senderId, string content, string projectId);
        Task<bool> IsRateLimitedAsync(string senderId);
        Task<List<string>> GetSpamKeywordsAsync();
        Task<bool> ContainsSpamKeywordsAsync(string content);
    }
}
