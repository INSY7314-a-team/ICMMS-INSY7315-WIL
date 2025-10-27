using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IContractorService
    {
        Task<ContractorDashboardViewModel> GetDashboardDataAsync();
        Task<List<ContractorTaskDto>> GetAssignedTasksAsync();
        Task<ContractorTaskDto?> GetTaskWithProjectAsync(string taskId);
        Task<ProgressReportDto> SubmitProgressReportAsync(ProgressReportDto report);
        Task<ContractorCompletionResultDto> RequestCompletionAsync(
            string taskId,
            string notes,
            string? documentId
        );
        Task<List<ProgressReportDto>> GetProgressReportsAsync(string taskId);
        Task<ProjectBudgetDto> GetTaskProjectBudgetAsync(string taskId);
        Task<CompletionReportDto> SubmitCompletionReportAsync(CompletionReportDto report);
        Task<List<CompletionReportDto>> GetCompletionReportsAsync(string taskId);
    }
}
