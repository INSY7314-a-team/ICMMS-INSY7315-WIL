using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ContractorTaskDetailViewModel
    {
        [JsonPropertyName("task")]
        public ContractorTaskDto Task { get; set; } = new();

        [JsonPropertyName("client")]
        public UserDto? Client { get; set; }

        [JsonPropertyName("projectManager")]
        public UserDto? ProjectManager { get; set; }

        [JsonPropertyName("project")]
        public ProjectDto? Project { get; set; }
    }
}
