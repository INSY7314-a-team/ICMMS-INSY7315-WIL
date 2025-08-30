using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Project Manager,Tester")]
    public class ProjectManagerController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public ProjectManagerController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public IActionResult Dashboard()
        {
            var model = new DashboardViewModel
            {
                Projects = GetSampleProjects(),
                Metrics = GetDashboardMetrics(),
                RecentActivities = GetRecentActivities(),
            };
            return View(model);
        }

        public IActionResult Timeline()
        {
            var model = new TimelineViewModel { Projects = GetSampleProjects() };
            return View(model);
        }

        public IActionResult Contractors()
        {
            var model = new ContractorViewModel { Contractors = GetSampleContractors() };
            return View(model);
        }

        public IActionResult Files()
        {
            var model = new FileReviewViewModel { Documents = GetSampleDocuments() };
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }

        private List<Project> GetSampleProjects()
        {
            return new List<Project>
            {
                new Project
                {
                    Id = "1",
                    Name = "Sandton Office Complex",
                    Location = "Sandton, Johannesburg",
                    Status = ProjectStatus.OnTrack,
                    Budget = 1200000,
                    Spent = 850000,
                    StartDate = new DateTime(2024, 1, 1),
                    DueDate = new DateTime(2024, 6, 30),
                    CompletedTasks = 45,
                    TotalTasks = 60,
                    Contractors = 8,
                    Phases = new List<ProjectPhase>
                    {
                        new ProjectPhase
                        {
                            Name = "Foundation",
                            Description = "Excavation and foundation work",
                            Status = PhaseStatus.Completed,
                            Duration = "6 weeks",
                            Budget = 300000,
                        },
                        new ProjectPhase
                        {
                            Name = "Structure",
                            Description = "Steel and concrete construction",
                            Status = PhaseStatus.InProgress,
                            Duration = "12 weeks",
                            Budget = 600000,
                        },
                        new ProjectPhase
                        {
                            Name = "Finishing",
                            Description = "Interior and exterior finishing",
                            Status = PhaseStatus.Pending,
                            Duration = "8 weeks",
                            Budget = 300000,
                        },
                    },
                },
                new Project
                {
                    Id = "2",
                    Name = "Cape Town Residential",
                    Location = "Camps Bay, Cape Town",
                    Status = ProjectStatus.AtRisk,
                    Budget = 800000,
                    Spent = 520000,
                    StartDate = new DateTime(2024, 2, 15),
                    DueDate = new DateTime(2024, 8, 15),
                    CompletedTasks = 28,
                    TotalTasks = 45,
                    Contractors = 6,
                    Phases = new List<ProjectPhase>
                    {
                        new ProjectPhase
                        {
                            Name = "Planning",
                            Description = "Permits and design finalization",
                            Status = PhaseStatus.Completed,
                            Duration = "4 weeks",
                            Budget = 100000,
                        },
                        new ProjectPhase
                        {
                            Name = "Construction",
                            Description = "Main construction phase",
                            Status = PhaseStatus.Delayed,
                            Duration = "16 weeks",
                            Budget = 500000,
                        },
                        new ProjectPhase
                        {
                            Name = "Landscaping",
                            Description = "Garden and exterior work",
                            Status = PhaseStatus.Pending,
                            Duration = "6 weeks",
                            Budget = 200000,
                        },
                    },
                },
                new Project
                {
                    Id = "3",
                    Name = "Pretoria Commercial",
                    Location = "Centurion, Pretoria",
                    Status = ProjectStatus.Delayed,
                    Budget = 950000,
                    Spent = 680000,
                    StartDate = new DateTime(2023, 11, 1),
                    DueDate = new DateTime(2024, 5, 30),
                    CompletedTasks = 32,
                    TotalTasks = 55,
                    Contractors = 7,
                    Phases = new List<ProjectPhase>
                    {
                        new ProjectPhase
                        {
                            Name = "Site Prep",
                            Description = "Site preparation and utilities",
                            Status = PhaseStatus.Completed,
                            Duration = "3 weeks",
                            Budget = 150000,
                        },
                        new ProjectPhase
                        {
                            Name = "Building",
                            Description = "Main building construction",
                            Status = PhaseStatus.Delayed,
                            Duration = "20 weeks",
                            Budget = 650000,
                        },
                        new ProjectPhase
                        {
                            Name = "Fit-out",
                            Description = "Interior fit-out and systems",
                            Status = PhaseStatus.Pending,
                            Duration = "8 weeks",
                            Budget = 150000,
                        },
                    },
                },
            };
        }

        private DashboardMetrics GetDashboardMetrics()
        {
            return new DashboardMetrics
            {
                TotalProjects = 12,
                ActiveBudget = 2400000,
                ActiveContractors = 28,
                PendingReviews = 7,
            };
        }

        private List<RecentActivity> GetRecentActivities()
        {
            return new List<RecentActivity>
            {
                new RecentActivity
                {
                    Type = "document",
                    Title = "Document Approved",
                    Description = "Floor plans for Sandton Office Complex",
                    Time = "2 hours ago",
                },
                new RecentActivity
                {
                    Type = "contractor",
                    Title = "Contractor Assigned",
                    Description = "Mike Johnson assigned to electrical work",
                    Time = "4 hours ago",
                },
                new RecentActivity
                {
                    Type = "budget",
                    Title = "Budget Updated",
                    Description = "Cape Town Residential - Phase 2",
                    Time = "6 hours ago",
                },
                new RecentActivity
                {
                    Type = "communication",
                    Title = "Client Communication",
                    Description = "Update sent to Pinnacle Properties",
                    Time = "1 day ago",
                },
            };
        }

        private List<Contractor> GetSampleContractors()
        {
            return new List<Contractor>
            {
                new Contractor
                {
                    Id = 1,
                    Name = "Mike Johnson",
                    Specialty = "Electrical",
                    Rating = 4.8m,
                    Phone = "+27 82 123 4567",
                    Email = "mike.johnson@email.com",
                    Location = "Johannesburg",
                    Status = ContractorStatus.Active,
                    CurrentProject = "Sandton Office Complex",
                    TasksCompleted = 15,
                    TasksTotal = 18,
                    Avatar = "MJ",
                },
                new Contractor
                {
                    Id = 2,
                    Name = "Sarah Williams",
                    Specialty = "Plumbing",
                    Rating = 4.9m,
                    Phone = "+27 83 987 6543",
                    Email = "sarah.williams@email.com",
                    Location = "Cape Town",
                    Status = ContractorStatus.Active,
                    CurrentProject = "Cape Town Residential",
                    TasksCompleted = 22,
                    TasksTotal = 25,
                    Avatar = "SW",
                },
                new Contractor
                {
                    Id = 3,
                    Name = "David Chen",
                    Specialty = "Construction",
                    Rating = 4.7m,
                    Phone = "+27 84 555 1234",
                    Email = "david.chen@email.com",
                    Location = "Pretoria",
                    Status = ContractorStatus.Available,
                    CurrentProject = null,
                    TasksCompleted = 45,
                    TasksTotal = 50,
                    Avatar = "DC",
                },
            };
        }

        private List<Document> GetSampleDocuments()
        {
            return new List<Document>
            {
                new Document
                {
                    Id = 1,
                    Name = "Structural Plans - Sandton Office",
                    Type = "PDF",
                    Size = "2.4 MB",
                    UploadedBy = "John Contractor",
                    UploadDate = new DateTime(2024, 1, 15),
                    Status = DocumentStatus.Pending,
                    Project = "Sandton Office Complex",
                    Priority = DocumentPriority.High,
                },
                new Document
                {
                    Id = 2,
                    Name = "Material Specifications",
                    Type = "DOCX",
                    Size = "1.2 MB",
                    UploadedBy = "Sarah Williams",
                    UploadDate = new DateTime(2024, 1, 14),
                    Status = DocumentStatus.Approved,
                    Project = "Cape Town Residential",
                    Priority = DocumentPriority.Medium,
                },
                new Document
                {
                    Id = 3,
                    Name = "Safety Inspection Report",
                    Type = "PDF",
                    Size = "3.1 MB",
                    UploadedBy = "Mike Johnson",
                    UploadDate = new DateTime(2024, 1, 13),
                    Status = DocumentStatus.Pending,
                    Project = "Pretoria Commercial",
                    Priority = DocumentPriority.High,
                },
            };
        }
    }
}
