using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IContractorService
    {
        Task<ContractorDashboardViewModel> GetDashboardDataAsync(string userId);
        Task<List<ContractorTaskDto>> GetAssignedTasksAsync(string userId);
        Task<ContractorTaskDto?> GetTaskWithProjectAsync(string taskId, string userId);
        Task<ProgressReportDto> SubmitProgressReportAsync(ProgressReportDto report, string userId);
        Task<object> RequestCompletionAsync(
            string taskId,
            string notes,
            string? documentId,
            string userId
        );
        Task<List<ProgressReportDto>> GetProgressReportsAsync(string taskId, string userId);
        Task<object> GetTaskProjectBudgetAsync(string taskId, string userId);
    }
}
