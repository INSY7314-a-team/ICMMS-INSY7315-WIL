using ICCMS_API.Controllers;
using Microsoft.Extensions.Configuration;
using Supabase;

namespace ICCMS_API.Services;

public class SupabaseService : ISupabaseService
{
    public Client SupabaseClient { get; }
    public IFirebaseService FirebaseService { get; }

    public SupabaseService(IConfiguration configuration, IFirebaseService firebaseService)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var supabaseAnonKey = configuration["Supabase:AnonKey"];
        FirebaseService = firebaseService;

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
        {
            throw new InvalidOperationException(
                "Supabase configuration is missing. Please check appsettings.json"
            );
        }

        var options = new SupabaseOptions { AutoConnectRealtime = true, AutoRefreshToken = true };

        SupabaseClient = new Client(supabaseUrl, supabaseAnonKey, options);

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
            Console.WriteLine($"SupabaseService.DownloadFileAsync called with bucket: {bucketName}, fileName: {fileName}");
            
            var bucket = SupabaseClient.Storage.From(bucketName);
            Console.WriteLine($"Got bucket reference for: {bucketName}");
            
            // First, let's check if the file exists by listing files
            var allFiles = await bucket.List("");
            var fileExists = allFiles.Any(f => f.Name == fileName);
            Console.WriteLine($"File '{fileName}' exists in bucket: {fileExists}");
            
            if (!fileExists)
            {
                Console.WriteLine($"File '{fileName}' not found in bucket '{bucketName}'");
                Console.WriteLine($"Available files: {string.Join(", ", allFiles.Select(f => f.Name))}");
                return Array.Empty<byte>();
            }
            
            var result = await bucket.Download(fileName, null);
            Console.WriteLine($"Download result: {(result != null ? $"{result.Length} bytes" : "null")}");

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
            var result = await bucket.Remove(fileName);

            return result != null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting file from Supabase: {ex.Message}", ex);
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
}
