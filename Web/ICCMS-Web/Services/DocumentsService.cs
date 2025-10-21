using System.Security.Claims;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IDocumentsService
    {
        Task<List<DocumentDto>> GetProjectDocumentsAsync(string projectId, ClaimsPrincipal user);
    }

    public class DocumentsService : IDocumentsService
    {
        private readonly IApiClient _apiClient;

        public DocumentsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<DocumentDto>> GetProjectDocumentsAsync(
            string projectId,
            ClaimsPrincipal user
        )
        {
            var docs =
                await _apiClient.GetAsync<List<DocumentDto>>(
                    $"/api/documents/project/{projectId}",
                    user
                ) ?? new List<DocumentDto>();

            // Normalize url expected by UI from fileUrl if needed
            foreach (var d in docs)
            {
                if (string.IsNullOrWhiteSpace(d.Url) && !string.IsNullOrWhiteSpace(d.FileUrl))
                {
                    d.Url = d.FileUrl!;
                }
            }

            return docs;
        }
    }
}
