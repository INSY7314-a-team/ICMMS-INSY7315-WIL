using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Route("Upload")]
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _env;

        public UploadController(ILogger<UploadController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpPost("Media")]
        [RequestSizeLimit(20_000_000)] // 20 MB limit
        public async Task<IActionResult> Media(IFormFile file)
        {
            _logger.LogInformation("🧱 [Upload/Media] Triggered");

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            try
            {
                // Ensure upload folder exists
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Generate safe unique filename
                var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, uniqueName);

                // Save file
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                // Build public URL
                var fileUrl = $"/uploads/{uniqueName}";
                _logger.LogInformation("✅ File uploaded successfully: {Url}", fileUrl);

                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [Upload/Media] Upload failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
