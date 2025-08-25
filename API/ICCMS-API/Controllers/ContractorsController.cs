using System.Linq;
using System.Security.Claims;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Contractor")]
    public class ContractorsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public ContractorsController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        [HttpGet("project/tasks")]
        public async Task<ActionResult<List<ProjectTask>>> GetProjectTasks()
        {
            try
            {
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var contractorTasks = tasks
                    .Where(t => t.AssignedTo == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(contractorTasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/phases")]
        public async Task<ActionResult<List<Phase>>> GetProjectPhases()
        {
            try
            {
                var phases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var contractorPhases = phases
                    .Where(p => p.AssignedTo == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(contractorPhases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("project/update/task")]
        public async Task<ActionResult<ProjectTask>> UpdateProjectTask([FromBody] ProjectTask task)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("tasks", task.TaskId, task);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
