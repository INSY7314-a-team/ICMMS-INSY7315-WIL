using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access notifications
    public class NotificationsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public NotificationsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Notification>>> GetNotifications()
        {
            try
            {
                var notifications = await _firebaseService.GetCollectionAsync<Notification>(
                    "notifications"
                );
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetNotification(string id)
        {
            try
            {
                var notification = await _firebaseService.GetDocumentAsync<Notification>(
                    "notifications",
                    id
                );
                if (notification == null)
                    return NotFound();
                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Notification>>> GetNotificationsByUser(string userId)
        {
            try
            {
                var notifications = await _firebaseService.GetCollectionAsync<Notification>(
                    "notifications"
                );
                var userNotifications = notifications.Where(n => n.UserId == userId).ToList();
                return Ok(userNotifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("user/{userId}/unread")]
        public async Task<ActionResult<List<Notification>>> GetUnreadNotificationsByUser(
            string userId
        )
        {
            try
            {
                var notifications = await _firebaseService.GetCollectionAsync<Notification>(
                    "notifications"
                );
                var unreadNotifications = notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToList();
                return Ok(unreadNotifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateNotification(
            [FromBody] Notification notification
        )
        {
            try
            {
                notification.CreatedAt = DateTime.UtcNow;
                var notificationId = await _firebaseService.AddDocumentAsync(
                    "notifications",
                    notification
                );
                return Ok(notificationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotification(
            string id,
            [FromBody] Notification notification
        )
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("notifications", id, notification);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            try
            {
                var notification = await _firebaseService.GetDocumentAsync<Notification>(
                    "notifications",
                    id
                );
                if (notification == null)
                    return NotFound();

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _firebaseService.UpdateDocumentAsync("notifications", id, notification);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("notifications", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
