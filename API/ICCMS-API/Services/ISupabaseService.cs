using Supabase;

namespace ICCMS_API.Services;

public interface ISupabaseService
{
    Client SupabaseClient { get; }
    Task<string> UploadFileAsync(
        string bucketName,
        string fileName,
        Stream fileStream,
        string contentType
    );
    Task<byte[]> DownloadFileAsync(string bucketName, string fileName);
    Task<bool> DeleteFileAsync(string bucketName, string fileName);
    Task<List<string>> ListFilesAsync(string bucketName, string? folderPath = null);
}
