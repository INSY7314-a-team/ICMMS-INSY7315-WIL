using System;
using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class DocumentsViewModel
    {
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();
        public List<ProjectSummary> AvailableProjects { get; set; } = new List<ProjectSummary>();
        public List<UserSummary> AvailableUsers { get; set; } = new List<UserSummary>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalDocuments { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public string CurrentProjectFilter { get; set; } = "all";
        public string CurrentCategoryFilter { get; set; } = "all";
        public string CurrentSearchTerm { get; set; } = "";
    }

    public class DocumentViewModel
    {
        public string FileName { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string Description { get; set; } = "";
        public string UploadedBy { get; set; } = "";
        public DateTime UploadedAt { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; } = "";
        public DateTime? ApprovedAt { get; set; }
    }

}
