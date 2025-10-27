using ICCMS_API.Models;
using System.Text.Json;

namespace ICCMS_API.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IFirebaseService _firebaseService;

    public AuditLogService(IFirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    public async Task LogAsync(string logType, string title, string description, string userId, string entityId)
    {
        try
        {
            Console.WriteLine($"[AuditLogService] ===== STARTING AUDIT LOG CREATION =====");
            Console.WriteLine($"[AuditLogService] LogType: {logType}, Title: {title}");
            Console.WriteLine($"[AuditLogService] UserId: {userId}, EntityId: {entityId}");
            
            if (_firebaseService == null)
            {
                Console.WriteLine($"[AuditLogService] ERROR: _firebaseService is NULL!");
                return;
            }
            
            var auditLog = new AuditLog
            {
                LogType = logType,
                Title = title,
                Description = description,
                UserId = userId,
                EntityId = entityId,
                TimestampUtc = DateTime.UtcNow
            };

            Console.WriteLine($"[AuditLogService] Created AuditLog object, now calling AddDocumentAsync...");
            var id = await _firebaseService.AddDocumentAsync("audit_logs", auditLog);
            
            Console.WriteLine($"[AuditLogService] AddDocumentAsync completed. Returned ID: '{id}'");
            
            if (string.IsNullOrEmpty(id))
            {
                Console.WriteLine($"[AuditLogService] WARNING: AddDocumentAsync returned NULL or EMPTY ID");
            }
            else
            {
                // Update the audit log with the ID returned from Firestore
                auditLog.Id = id;
                
                // Update the document in Firestore to save the ID field
                await _firebaseService.UpdateDocumentAsync("audit_logs", id, auditLog);
                
                Console.WriteLine($"[AuditLogService] SUCCESS: Audit log created with ID: {id}");
            }
            
            Console.WriteLine($"[AuditLogService] ===== AUDIT LOG CREATION COMPLETED =====");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogService] ===== EXCEPTION CAUGHT =====");
            Console.WriteLine($"[AuditLogService] ERROR Message: {ex.Message}");
            Console.WriteLine($"[AuditLogService] ERROR Type: {ex.GetType().Name}");
            Console.WriteLine($"[AuditLogService] ERROR Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[AuditLogService] ERROR Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"[AuditLogService] ERROR Inner Stack: {ex.InnerException.StackTrace}");
            }
            Console.WriteLine($"[AuditLogService] ===== EXCEPTION DETAILS END =====");
        }
    }
}
