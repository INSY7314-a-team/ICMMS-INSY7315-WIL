namespace ICCMS_API.Services;

public interface IAuditLogService
{
    Task LogAsync(string logType, string title, string description, string userId, string entityId);
}
