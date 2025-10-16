using System.Text.Json;

namespace ICCMS_API.Services
{
    public class SupabaseBlueprintService
    {
        private readonly HttpClient _httpClient;

        public SupabaseBlueprintService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Downloads a blueprint from Supabase and converts it to base64
        /// </summary>
        /// <param name="supabaseUrl">The signed Supabase URL</param>
        /// <returns>Base64 encoded file data</returns>
        public async Task<string> DownloadBlueprintAsBase64Async(string supabaseUrl)
        {
            try
            {
                Console.WriteLine($"Downloading blueprint from Supabase: {supabaseUrl}");
                
                var response = await _httpClient.GetAsync(supabaseUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to download blueprint from Supabase: {response.StatusCode} - {response.ReasonPhrase}");
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var base64Data = Convert.ToBase64String(fileBytes);
                
                Console.WriteLine($"Successfully downloaded {fileBytes.Length} bytes from Supabase");
                return base64Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading blueprint from Supabase: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines file type from Supabase URL
        /// </summary>
        /// <param name="supabaseUrl">The Supabase URL</param>
        /// <returns>File type (pdf, image, dwg, dxf)</returns>
        public string GetFileTypeFromSupabaseUrl(string supabaseUrl)
        {
            // Extract file extension from URL (before query parameters)
            var urlWithoutQuery = supabaseUrl.Split('?')[0];
            var extension = Path.GetExtension(urlWithoutQuery).ToLower();
            
            return extension switch
            {
                ".pdf" => "pdf",
                ".jpg" or ".jpeg" => "image",
                ".png" => "image",
                ".dwg" => "dwg",
                ".dxf" => "dxf",
                _ => "pdf" // Default to PDF
            };
        }

        /// <summary>
        /// Validates if a URL is a valid Supabase storage URL
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if it's a valid Supabase URL</returns>
        public bool IsValidSupabaseUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && 
                   (url.Contains("supabase.co/storage") || url.Contains("supabase.com/storage"));
        }

        /// <summary>
        /// Extracts project context from Supabase URL metadata (if available)
        /// </summary>
        /// <param name="supabaseUrl">The Supabase URL</param>
        /// <returns>Project context object</returns>
        public object GetProjectContextFromUrl(string supabaseUrl)
        {
            // Extract filename from URL to infer project details
            var urlWithoutQuery = supabaseUrl.Split('?')[0];
            var fileName = Path.GetFileNameWithoutExtension(urlWithoutQuery);
            
            return new
            {
                projectId = $"SUPABASE_{DateTime.Now:yyyyMMdd}_{fileName}",
                projectType = "residential", // Default, could be inferred from filename
                buildingType = "single_family_home", // Default
                location = "South Africa", // Default
                squareFootage = 2500, // Default
                source = "supabase",
                fileName = fileName,
                downloadDate = DateTime.UtcNow
            };
        }
    }
}
