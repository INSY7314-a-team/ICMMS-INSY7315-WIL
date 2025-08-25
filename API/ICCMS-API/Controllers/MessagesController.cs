using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public MessagesController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Message>>> GetMessages()
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Message>> GetMessage(string id)
        {
            try
            {
                var message = await _firebaseService.GetDocumentAsync<Message>("messages", id);
                if (message == null)
                    return NotFound();
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByUser(string userId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var userMessages = messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).ToList();
                return Ok(userMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByProject(string projectId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var projectMessages = messages.Where(m => m.ProjectId == projectId).ToList();
                return Ok(projectMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateMessage([FromBody] Message message)
        {
            try
            {
                message.SentAt = DateTime.UtcNow;
                var messageId = await _firebaseService.AddDocumentAsync("messages", message);
                return Ok(messageId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(string id, [FromBody] Message message)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("messages", id, message);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("messages", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
