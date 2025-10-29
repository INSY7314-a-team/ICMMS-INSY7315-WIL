using ICCMS_API.Controllers;
using Microsoft.Extensions.Configuration;
using Supabase;

namespace ICCMS_API.Services;

public class SupabaseService : ISupabaseService
{
    public Client SupabaseClient { get; }
    public IFirebaseService FirebaseService { get; }
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;
    private readonly string _supabaseServiceKey;

    public SupabaseService(IConfiguration configuration, IFirebaseService firebaseService)
    {
        _supabaseUrl = configuration["Supabase:Url"];
        _supabaseAnonKey = configuration["Supabase:AnonKey"];
        _supabaseServiceKey = configuration["Supabase:ServiceKey"];
        FirebaseService = firebaseService;

        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseAnonKey))
        {
            throw new InvalidOperationException(
                "Supabase configuration is missing. Please check appsettings.json"
            );
        }

        var options = new SupabaseOptions { AutoConnectRealtime = true, AutoRefreshToken = true };

        SupabaseClient = new Client(_supabaseUrl, _supabaseAnonKey, options);

        // Initialize the client
        SupabaseClient.InitializeAsync().Wait();
    }

    public async Task<string> UploadFileAsync(
        string bucketName,
        string fileName,
        Stream fileStream,
        string contentType
    )
    {
        try
        {
            Console.WriteLine(
                $"SupabaseService.UploadFileAsync called with bucket: {bucketName}, fileName: {fileName}, contentType: {contentType}"
            );

            // Convert stream to byte array
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            Console.WriteLine($"File converted to byte array. Size: {fileBytes.Length} bytes");

            var bucket = SupabaseClient.Storage.From(bucketName);
            Console.WriteLine($"Got bucket reference for: {bucketName}");

            await bucket.Upload(
                fileBytes,
                fileName,
                new Supabase.Storage.FileOptions { ContentType = contentType, Upsert = true }
            );

            Console.WriteLine("File uploaded to Supabase successfully");

            var publicUrl = bucket.GetPublicUrl(fileName);
            Console.WriteLine($"Generated public URL: {publicUrl}");

            return publicUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SupabaseService.UploadFileAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Error uploading file to Supabase: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DownloadFileAsync(string bucketName, string fileName)
    {
        try
        {
            Console.WriteLine(
                $"SupabaseService.DownloadFileAsync called with bucket: {bucketName}, fileName: {fileName}"
            );

            var bucket = SupabaseClient.Storage.From(bucketName);
            Console.WriteLine($"Got bucket reference for: {bucketName}");

            // First, let's check if the file exists by listing files
            var allFiles = await bucket.List("");
            var fileExists = allFiles.Any(f => f.Name == fileName);
            Console.WriteLine($"File '{fileName}' exists in bucket: {fileExists}");

            if (!fileExists)
            {
                Console.WriteLine($"File '{fileName}' not found in bucket '{bucketName}'");
                Console.WriteLine(
                    $"Available files: {string.Join(", ", allFiles.Select(f => f.Name))}"
                );
                return Array.Empty<byte>();
            }

            var result = await bucket.Download(fileName, null);
            Console.WriteLine(
                $"Download result: {(result != null ? $"{result.Length} bytes" : "null")}"
            );

            return result ?? Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SupabaseService.DownloadFileAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Error downloading file from Supabase: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string bucketName, string fileName)
    {
        try
        {
            var bucket = SupabaseClient.Storage.From(bucketName);
            Console.WriteLine($"[Supabase] Bucket: {bucket}");
            // Log Supabase client info for debugging
            Console.WriteLine(
                $"[Supabase] DeleteFileAsync: Using Supabase client for bucket: {bucketName}"
            );

            // Check if file exists; if not, consider delete successful (idempotent)
            try
            {
                var files = await bucket.List("");
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Found {files?.Count ?? 0} files in bucket '{bucketName}'"
                );

                if (files != null)
                {
                    Console.WriteLine($"[Supabase] DeleteFileAsync: Files in bucket:");
                    foreach (var file in files.Take(10)) // Show first 10 files
                    {
                        Console.WriteLine($"[Supabase] DeleteFileAsync:   - {file.Name}");
                    }
                }

                if (
                    files == null
                    || !files.Any(f =>
                        string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    Console.WriteLine(
                        $"[Supabase] DeleteFileAsync: '{fileName}' not found in bucket '{bucketName}', treating as success."
                    );
                    return true;
                }
                else
                {
                    Console.WriteLine(
                        $"[Supabase] DeleteFileAsync: '{fileName}' found in bucket, proceeding with deletion."
                    );
                }
            }
            catch (Exception listEx)
            {
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: list failed, continuing to attempt remove. {listEx.Message}"
                );
            }

            // Call Remove with single path per current client signature
            try
            {
                // Try different approaches for deletion
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Attempting deletion with single string parameter..."
                );

                // Log Supabase client configuration for debugging
                Console.WriteLine($"[Supabase] DeleteFileAsync: Supabase URL: {_supabaseUrl}");
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Supabase Anon Key: {_supabaseAnonKey?.Substring(0, Math.Min(20, _supabaseAnonKey.Length))}..."
                );

                // Check bucket permissions and configuration
                Console.WriteLine($"[Supabase] DeleteFileAsync: Using bucket: {bucketName}");

                // Try different path formats
                var pathsToTry = new[] { fileName, $"/{fileName}", fileName.TrimStart('/') };

                foreach (var path in pathsToTry)
                {
                    Console.WriteLine($"[Supabase] DeleteFileAsync: Trying path: '{path}'");
                    try
                    {
                        // Log the exact method call
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: Calling bucket.Remove('{path}')"
                        );

                        var removeResult = await bucket.Remove(path);
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: remove called for '{path}' in '{bucketName}'. Result: {removeResult?.ToString() ?? "null"}"
                        );

                        // Check if the result is meaningful
                        if (removeResult == null)
                        {
                            Console.WriteLine(
                                $"[Supabase] DeleteFileAsync: Remove returned null - this could indicate:"
                            );
                            Console.WriteLine(
                                $"[Supabase] DeleteFileAsync: 1. Permissions issue (RLS policy blocking deletion)"
                            );
                            Console.WriteLine(
                                $"[Supabase] DeleteFileAsync: 2. Method not working as expected"
                            );
                            Console.WriteLine(
                                $"[Supabase] DeleteFileAsync: 3. File path format issue"
                            );
                        }
                        else
                        {
                            Console.WriteLine(
                                $"[Supabase] DeleteFileAsync: Remove returned non-null result: {removeResult}"
                            );
                        }

                        // For now, let's assume the deletion worked if no exception was thrown
                        // and let the verification step determine if it actually worked
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: Proceeding to verification step..."
                        );
                        break;
                    }
                    catch (Exception pathEx)
                    {
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: Path '{path}' failed: {pathEx.Message}"
                        );
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: Exception type: {pathEx.GetType().Name}"
                        );
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: Stack trace: {pathEx.StackTrace}"
                        );
                    }
                }

                // Verify deletion by checking if file still exists
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Verifying deletion by checking if file still exists..."
                );
                try
                {
                    var filesAfterDelete = await bucket.List("");
                    var fileStillExists =
                        filesAfterDelete?.Any(f =>
                            string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase)
                        ) ?? false;

                    if (fileStillExists)
                    {
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: File '{fileName}' still exists after Remove call - trying alternative deletion method"
                        );

                        // Try alternative deletion using direct HTTP call
                        return await TryAlternativeDeletion(bucketName, fileName);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[Supabase] DeleteFileAsync: File '{fileName}' no longer exists - deletion successful"
                        );
                        return true;
                    }
                }
                catch (Exception verifyEx)
                {
                    Console.WriteLine(
                        $"[Supabase] DeleteFileAsync: Could not verify deletion: {verifyEx.Message}"
                    );
                    // If we can't verify, assume the Remove call worked if it didn't throw
                    return true;
                }
            }
            catch (Exception removeEx)
            {
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Remove threw exception for '{fileName}': {removeEx.Message}"
                );
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Exception type: {removeEx.GetType().Name}"
                );
                Console.WriteLine(
                    $"[Supabase] DeleteFileAsync: Stack trace: {removeEx.StackTrace}"
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Supabase] Error deleting '{fileName}' from bucket '{bucketName}': {ex.Message}"
            );
            return false;
        }
    }

    public async Task<List<string>> ListFilesAsync(string bucketName, string? folderPath = null)
    {
        try
        {
            var bucket = SupabaseClient.Storage.From(bucketName);
            var result = await bucket.List(folderPath ?? "");

            if (result == null)
                return new List<string>();

            return result.Where(f => f.Name != null).Select(f => f.Name!).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error listing files from Supabase: {ex.Message}", ex);
        }
    }

    public async Task<bool> EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            Console.WriteLine($"Checking if bucket '{bucketName}' exists...");

            // Try to list files in the bucket to see if it exists
            var bucket = SupabaseClient.Storage.From(bucketName);
            await bucket.List("");

            Console.WriteLine($"Bucket '{bucketName}' exists and is accessible");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Bucket '{bucketName}' does not exist or is not accessible: {ex.Message}"
            );
            return false;
        }
    }

    public async Task<bool> CreateBucketAsync(string bucketName, bool isPublic = true)
    {
        try
        {
            Console.WriteLine($"Creating bucket '{bucketName}' with public access: {isPublic}");

            // Note: The Supabase .NET client doesn't have a direct method to create buckets
            // This would typically be done through the Supabase dashboard or REST API
            // For now, we'll return false and suggest manual creation
            Console.WriteLine(
                $"Bucket creation not supported via .NET client. Please create bucket '{bucketName}' manually in Supabase dashboard."
            );
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating bucket '{bucketName}': {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TryAlternativeDeletion(string bucketName, string fileName)
    {
        try
        {
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Attempting direct HTTP deletion for '{fileName}' in bucket '{bucketName}'"
            );

            using var httpClient = new HttpClient();

            // Try with anon key first
            var apiKey = _supabaseAnonKey;
            if (!string.IsNullOrEmpty(_supabaseServiceKey))
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: Using service key for deletion (has delete permissions)"
                );
                apiKey = _supabaseServiceKey;
            }
            else
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: Using anon key for deletion (may have permission issues)"
                );
            }

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("apikey", apiKey);

            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Headers set - Authorization: Bearer {apiKey.Substring(0, Math.Min(20, apiKey.Length))}..."
            );
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Headers set - apikey: {apiKey.Substring(0, Math.Min(20, apiKey.Length))}..."
            );

            // First, let's test if we can access the file via GET to verify authentication and path
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Testing file access via GET first..."
            );
            var testUrl =
                $"{_supabaseUrl}/storage/v1/object/{bucketName}/{Uri.EscapeDataString(fileName)}";
            try
            {
                var testResponse = await httpClient.GetAsync(testUrl);
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: GET test Status: {testResponse.StatusCode}"
                );
                if (testResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: GET test successful - file is accessible"
                    );
                }
                else
                {
                    var testContent = await testResponse.Content.ReadAsStringAsync();
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: GET test failed - {testContent}"
                    );
                }
            }
            catch (Exception testEx)
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: GET test exception: {testEx.Message}"
                );
            }

            // Also try to get file info via the list API to see the exact file structure
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Getting file info via list API..."
            );
            try
            {
                var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{bucketName}";
                var listResponse = await httpClient.PostAsync(
                    listUrl,
                    new StringContent(
                        "{\"limit\": 100, \"offset\": 0}",
                        System.Text.Encoding.UTF8,
                        "application/json"
                    )
                );
                var listContent = await listResponse.Content.ReadAsStringAsync();
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: List API Status: {listResponse.StatusCode}"
                );
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: List API Response: {listContent}"
                );
            }
            catch (Exception listEx)
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: List API exception: {listEx.Message}"
                );
            }

            // Try different URL encoding approaches
            var encodedFileName = Uri.EscapeDataString(fileName);
            var deleteUrl = $"{_supabaseUrl}/storage/v1/object/{bucketName}/{encodedFileName}";
            Console.WriteLine($"[Supabase] TryAlternativeDeletion: DELETE URL: {deleteUrl}");
            Console.WriteLine($"[Supabase] TryAlternativeDeletion: Original fileName: {fileName}");
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Encoded fileName: {encodedFileName}"
            );

            var response = await httpClient.DeleteAsync(deleteUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: HTTP Status: {response.StatusCode}"
            );
            Console.WriteLine($"[Supabase] TryAlternativeDeletion: Response: {responseContent}");

            // If the first attempt fails with 404, try alternative path formats
            if (
                response.StatusCode == System.Net.HttpStatusCode.BadRequest
                && responseContent.Contains("not_found")
            )
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: First attempt failed with 404, trying alternative path formats..."
                );

                // Try different path formats
                var alternativePaths = new[]
                {
                    fileName, // Original filename
                    $"/{fileName}", // With leading slash
                    fileName.TrimStart('/'), // Without leading slash
                    Uri.EscapeUriString(fileName), // Different encoding
                    fileName.Replace(" ", "%20"), // Manual space encoding
                };

                foreach (var altPath in alternativePaths)
                {
                    if (altPath == encodedFileName)
                        continue; // Skip the one we already tried

                    var altUrl = $"{_supabaseUrl}/storage/v1/object/{bucketName}/{altPath}";
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: Trying alternative URL: {altUrl}"
                    );

                    var altResponse = await httpClient.DeleteAsync(altUrl);
                    var altResponseContent = await altResponse.Content.ReadAsStringAsync();

                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: Alternative Status: {altResponse.StatusCode}"
                    );
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: Alternative Response: {altResponseContent}"
                    );

                    if (altResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            $"[Supabase] TryAlternativeDeletion: Alternative path succeeded!"
                        );
                        response = altResponse;
                        responseContent = altResponseContent;
                        break;
                    }
                }
            }

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Supabase] TryAlternativeDeletion: HTTP deletion successful");

                // Verify deletion
                await Task.Delay(1000); // Wait a moment for the deletion to propagate
                var bucket = SupabaseClient.Storage.From(bucketName);
                var filesAfterDelete = await bucket.List("");
                var fileStillExists =
                    filesAfterDelete?.Any(f =>
                        string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase)
                    ) ?? false;

                if (!fileStillExists)
                {
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: File successfully deleted via HTTP"
                    );
                    return true;
                }
                else
                {
                    Console.WriteLine(
                        $"[Supabase] TryAlternativeDeletion: File still exists after HTTP deletion"
                    );
                    return false;
                }
            }
            else
            {
                Console.WriteLine(
                    $"[Supabase] TryAlternativeDeletion: HTTP deletion failed with status {response.StatusCode}"
                );
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Supabase] TryAlternativeDeletion: Exception during HTTP deletion: {ex.Message}"
            );
            Console.WriteLine($"[Supabase] TryAlternativeDeletion: Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}
