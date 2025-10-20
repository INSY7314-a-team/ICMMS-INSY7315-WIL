using System.Security.Claims;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    /// <summary>
    /// Handles Project Phase/Task setup, status transitions, and guards.
    /// All logic is centralized to avoid controller duplication.
    /// </summary>
    public class ProjectSetupService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ProjectSetupService> _logger;

        public ProjectSetupService(IApiClient apiClient, ILogger<ProjectSetupService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ================================================================
        // 🧱 PHASE CREATION
        // ================================================================
        public async Task<(bool Success, string Message)> CreatePhaseAsync(string projectId, PhaseDto phase, ClaimsPrincipal user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phase.Name))
                    return (false, "Phase name is required.");

                phase.ProjectId = projectId;
                phase.PhaseId = string.IsNullOrEmpty(phase.PhaseId)
                    ? Guid.NewGuid().ToString()
                    : phase.PhaseId;

                var response = await _apiClient.PostAsync<PhaseDto>(
                    $"/api/projectmanager/create/project/{projectId}/phase", phase, user);

                if (response == null)
                    return (false, "Failed to create phase (API returned null).");

                return (true, $"Phase '{phase.Name}' added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating phase for project {Id}", projectId);
                return (false, $"Error creating phase: {ex.Message}");
            }
        }

        // ================================================================
        // 🧩 TASK CREATION
        // ================================================================
        public async Task<(bool Success, string Message)> CreateTaskAsync(string projectId, ProjectTaskDto task, ClaimsPrincipal user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(task.Name))
                    return (false, "Task name is required.");

                task.ProjectId = projectId;
                task.TaskId = string.IsNullOrEmpty(task.TaskId)
                    ? Guid.NewGuid().ToString()
                    : task.TaskId;

                var response = await _apiClient.PostAsync<ProjectTaskDto>(
                    $"/api/projectmanager/create/project/{projectId}/task", task, user);

                if (response == null)
                    return (false, "Failed to create task (API returned null).");

                return (true, $"Task '{task.Name}' added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating task for project {Id}", projectId);
                return (false, $"Error creating task: {ex.Message}");
            }
        }

        // ================================================================
        // 🚀 FINALIZE PROJECT (Draft → Active)
        // ================================================================
        public async Task<(bool Success, string Message)> FinalizeProjectAsync(ProjectDto project, ClaimsPrincipal user)
        {
            try
            {
                if (!string.Equals(project.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                    return (false, $"Cannot finalize project '{project.Name}' because it is not in Draft status.");

                project.Status = "Active";

                var updated = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/update/project/{project.ProjectId}", project, user);

                if (updated == null)
                    return (false, "Failed to finalize project (API returned null).");

                return (true, $"Project '{project.Name}' finalized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error finalizing project {Name}", project.Name);
                return (false, $"Error finalizing project: {ex.Message}");
            }
        }

        // ================================================================
        // 🏁 COMPLETE PROJECT (Active → Completed)
        // ================================================================
        public async Task<(bool Success, string Message)> CompleteProjectAsync(ProjectDto project, ClaimsPrincipal user)
        {
            try
            {
                if (!string.Equals(project.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    return (false, $"Cannot complete project '{project.Name}' because it is not Active.");

                project.Status = "Completed";
                project.EndDateActual = DateTime.UtcNow;

                var updated = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/update/project/{project.ProjectId}", project, user);

                if (updated == null)
                    return (false, "Failed to complete project (API returned null).");

                return (true, $"Project '{project.Name}' marked as completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error completing project {Name}", project.Name);
                return (false, $"Error completing project: {ex.Message}");
            }
        }
    }
}
