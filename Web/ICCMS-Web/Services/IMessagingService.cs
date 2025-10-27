using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IMessagingService
    {
        /// <summary>
        /// Send a direct message from one user to another
        /// </summary>
        Task<bool> SendDirectMessageAsync(
            string senderId,
            string receiverId,
            string projectId,
            string subject,
            string content
        );

        /// <summary>
        /// Get all message threads for a user based on their role
        /// </summary>
        Task<List<ThreadDto>> GetUserThreadsAsync(string userId, string userRole);

        /// <summary>
        /// Get all messages in a specific thread
        /// </summary>
        Task<List<MessageDto>> GetThreadMessagesAsync(string threadId, string userId);

        /// <summary>
        /// Get unread message count for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Mark a message as read
        /// </summary>
        Task<bool> MarkAsReadAsync(string messageId, string userId);

        /// <summary>
        /// Mark all messages in a thread as read
        /// </summary>
        Task<bool> MarkThreadAsReadAsync(string threadId, string userId);

        /// <summary>
        /// Validate if a user can send a message to another user based on communication hierarchy
        /// </summary>
        bool CanUserSendMessage(string senderRole, string receiverRole);
    }
}
