using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Tester")] // Admin oversight, PM management, Tester access
    public class AuditLogsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private const string Collection = "audit_logs";

        public AuditLogsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // Local input contract: what clients may send (no Id/timestamp)
        public class CreateAuditLogInput
        {
            public string LogType { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
        }

        // POST: api/auditlogs  (append-only)
        [HttpPost]
        public async Task<ActionResult<string>> Create([FromBody] CreateAuditLogInput input)
        {
            try
            {
                if (!Types.IsValidAuditLogType(input.LogType))
                    return BadRequest(
                        new
                        {
                            error = $"Invalid logType. Allowed: {string.Join(", ", Types.GetAuditLogTypes())}",
                        }
                    );

                var log = new AuditLog
                {
                    LogType = input.LogType,
                    Title = input.Title,
                    Description = input.Description,
                    UserId = input.UserId,
                    EntityId = input.EntityId,
                    TimestampUtc = DateTime.UtcNow, // server-set
                };

                var id = await _firebaseService.AddDocumentAsync(Collection, log);

                // Persist generated id into the document (handy for lookups)
                log.Id = id;
                await _firebaseService.UpdateDocumentAsync(Collection, id, log);

                return Ok(id);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/auditlogs?limit=200
        [HttpGet]
        public async Task<ActionResult<List<AuditLog>>> GetAll([FromQuery] int limit = 200)
        {
            try
            {
                if (limit <= 0 || limit > 1000)
                    limit = 200;

                var all = await _firebaseService.GetCollectionAsync<AuditLog>(Collection);
                var ordered = all.OrderByDescending(a => a.TimestampUtc).Take(limit).ToList();

                return Ok(ordered);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/auditlogs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLog>> GetById(string id)
        {
            try
            {
                var log = await _firebaseService.GetDocumentAsync<AuditLog>(Collection, id);
                if (log == null)
                    return NotFound(new { error = "Audit log not found" });
                return Ok(log);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/auditlogs/search?userId=&logType=&entityId=&fromUtc=&toUtc=&limit=
        [HttpGet("search")]
        public async Task<ActionResult<List<AuditLog>>> Search(
            [FromQuery] string? userId,
            [FromQuery] string? logType,
            [FromQuery] string? entityId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int limit = 200
        )
        {
            try
            {
                if (limit <= 0 || limit > 1000)
                    limit = 200;

                var all = await _firebaseService.GetCollectionAsync<AuditLog>(Collection);
                var q = all.AsQueryable();

                if (!string.IsNullOrWhiteSpace(userId))
                    q = q.Where(a => a.UserId == userId);

                if (!string.IsNullOrWhiteSpace(logType))
                    q = q.Where(a => a.LogType == logType);

                if (!string.IsNullOrWhiteSpace(entityId))
                    q = q.Where(a => a.EntityId == entityId);

                if (fromUtc.HasValue)
                    q = q.Where(a => a.TimestampUtc >= fromUtc.Value);

                if (toUtc.HasValue)
                    q = q.Where(a => a.TimestampUtc < toUtc.Value);

                var results = q.OrderByDescending(a => a.TimestampUtc).Take(limit).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/auditlogs/by-entity/{entityId}
        [HttpGet("by-entity/{entityId}")]
        public async Task<ActionResult<List<AuditLog>>> ByEntity(
            string entityId,
            [FromQuery] int limit = 200
        )
        {
            try
            {
                if (limit <= 0 || limit > 1000)
                    limit = 200;

                var all = await _firebaseService.GetCollectionAsync<AuditLog>(Collection);
                var results = all.Where(a => a.EntityId == entityId)
                    .OrderByDescending(a => a.TimestampUtc)
                    .Take(limit)
                    .ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/auditlogs/types
        [HttpGet("types")]
        public ActionResult<IEnumerable<string>> GetTypes() => Ok(Types.GetAuditLogTypes());

        // NOTE: No Update, No Delete — append-only by design.
    }
}
