using ICCMS_API.Models.Messages;

namespace ICCMS_API.Services;

public interface IMessageDetailService
{
    Task<MessageDashboardDataDto> BuildAdminDashboardAsync(MessageDashboardQuery query);

    Task<UserMessagesDetailDto> BuildUserMessagesDetailAsync(string userId, string userRole);

    Task<MessageThreadDetailDto?> BuildThreadDetailAsync(string threadId);
}

public class MessageDashboardQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? ProjectId { get; set; }
    public string? ThreadType { get; set; }
    public string? UserId { get; set; }
    public string? ReadStatus { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
