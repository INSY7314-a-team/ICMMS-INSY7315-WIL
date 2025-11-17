using ICCMS_API.Models;
using ICCMS_API.Models.ProjectDetail;

namespace ICCMS_API.Services;

public interface IProjectDetailService
{
    Task<ProjectDetailDataDto> BuildClientProjectDetailAsync(Project project, string clientId);

    Task<ProjectDetailDataDto> BuildManagerProjectDetailAsync(Project project);
}
