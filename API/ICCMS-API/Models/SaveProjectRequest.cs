using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class SaveProjectRequest
    {
        [FirestoreProperty("project")]
        public Project Project { get; set; } = new Project();

        [FirestoreProperty("phases")]
        public List<Phase> Phases { get; set; } = new List<Phase>();

        [FirestoreProperty("tasks")]
        public List<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
