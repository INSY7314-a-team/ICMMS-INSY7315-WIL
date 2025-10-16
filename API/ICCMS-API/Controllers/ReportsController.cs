using System.Globalization;
using Google.Cloud.Firestore;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ISupabaseService _supabaseService;
        private readonly IAiProcessingService _aiProcessingService;

        public ReportsController(
            FirestoreDb firestoreDb,
            ISupabaseService supabaseService,
            IAiProcessingService aiProcessingService
        )
        {
            _firestoreDb = firestoreDb;
            _supabaseService = supabaseService;
            _aiProcessingService = aiProcessingService;
        }

        /// <summary>
        /// Get project status distribution for dashboard reporting
        /// </summary>
        [HttpGet("projects/status")]
        public async Task<IActionResult> GetProjectStatusReport(
            [FromQuery] string? projectManagerId = null
        )
        {
            try
            {
                var projectsCollection = _firestoreDb.Collection("projects");
                Query query = projectsCollection;

                if (!string.IsNullOrEmpty(projectManagerId))
                {
                    query = query.WhereEqualTo("projectManagerId", projectManagerId);
                }

                var snapshot = await query.GetSnapshotAsync();
                var projects = snapshot.Documents.Select(doc => doc.ConvertTo<Project>()).ToList();

                var statusReport = new
                {
                    TotalProjects = projects.Count,
                    StatusDistribution = projects
                        .GroupBy(p => p.Status)
                        .Select(g => new
                        {
                            Status = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / projects.Count * 100, 2),
                        })
                        .OrderByDescending(s => s.Count)
                        .ToList(),
                    ProjectsByStatus = projects
                        .GroupBy(p => p.Status)
                        .ToDictionary(
                            g => g.Key,
                            g =>
                                g.Select(p => new
                                    {
                                        p.ProjectId,
                                        p.Name,
                                        p.Status,
                                        p.StartDate,
                                        p.EndDatePlanned,
                                        p.EndDateActual,
                                        BudgetPlanned = p.BudgetPlanned,
                                        BudgetActual = p.BudgetActual,
                                    })
                                    .ToList()
                        ),
                };

                return Ok(statusReport);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving project status report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get budget vs actual analysis for projects
        /// </summary>
        [HttpGet("projects/budget-vs-actual")]
        public async Task<IActionResult> GetBudgetVsActualReport(
            [FromQuery] string? projectManagerId = null,
            [FromQuery] int? year = null
        )
        {
            try
            {
                var projectsCollection = _firestoreDb.Collection("projects");
                Query query = projectsCollection;

                if (!string.IsNullOrEmpty(projectManagerId))
                {
                    query = query.WhereEqualTo("projectManagerId", projectManagerId);
                }

                var snapshot = await query.GetSnapshotAsync();
                var projects = snapshot.Documents.Select(doc => doc.ConvertTo<Project>()).ToList();

                if (year.HasValue)
                {
                    projects = projects.Where(p => p.StartDate.Year == year.Value).ToList();
                }

                var budgetAnalysis = new
                {
                    TotalPlannedBudget = projects.Sum(p => p.BudgetPlanned),
                    TotalActualBudget = projects.Sum(p => p.BudgetActual),
                    BudgetVariance = projects.Sum(p => p.BudgetActual)
                        - projects.Sum(p => p.BudgetPlanned),
                    BudgetVariancePercentage = projects.Sum(p => p.BudgetPlanned) > 0
                        ? Math.Round(
                            (projects.Sum(p => p.BudgetActual) - projects.Sum(p => p.BudgetPlanned))
                                / projects.Sum(p => p.BudgetPlanned)
                                * 100,
                            2
                        )
                        : 0,
                    Projects = projects
                        .Select(p => new
                        {
                            p.ProjectId,
                            p.Name,
                            p.Status,
                            PlannedBudget = p.BudgetPlanned,
                            ActualBudget = p.BudgetActual,
                            Variance = p.BudgetActual - p.BudgetPlanned,
                            VariancePercentage = p.BudgetPlanned > 0
                                ? Math.Round(
                                    (p.BudgetActual - p.BudgetPlanned) / p.BudgetPlanned * 100,
                                    2
                                )
                                : 0,
                            IsOverBudget = p.BudgetActual > p.BudgetPlanned,
                        })
                        .OrderByDescending(p => Math.Abs(p.Variance))
                        .ToList(),
                    OverBudgetProjects = projects.Count(p => p.BudgetActual > p.BudgetPlanned),
                    UnderBudgetProjects = projects.Count(p => p.BudgetActual < p.BudgetPlanned),
                    OnBudgetProjects = projects.Count(p => p.BudgetActual == p.BudgetPlanned),
                };

                return Ok(budgetAnalysis);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving budget vs actual report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get project progress analysis
        /// </summary>
        [HttpGet("projects/progress")]
        public async Task<IActionResult> GetProjectProgressReport(
            [FromQuery] string? projectManagerId = null
        )
        {
            try
            {
                var projectsCollection = _firestoreDb.Collection("projects");
                var tasksCollection = _firestoreDb.Collection("projectTasks");

                Query projectsQuery = projectsCollection;
                if (!string.IsNullOrEmpty(projectManagerId))
                {
                    projectsQuery = projectsQuery.WhereEqualTo(
                        "projectManagerId",
                        projectManagerId
                    );
                }

                var projectsSnapshot = await projectsQuery.GetSnapshotAsync();
                var projects = projectsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Project>())
                    .ToList();

                var progressReport = new List<object>();

                foreach (var project in projects)
                {
                    var tasksQuery = tasksCollection.WhereEqualTo("projectId", project.ProjectId);
                    var tasksSnapshot = await tasksQuery.GetSnapshotAsync();
                    var tasks = tasksSnapshot
                        .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                        .ToList();

                    var completedTasks = tasks.Count(t => t.Status == "Completed");
                    var totalTasks = tasks.Count;
                    var averageProgress = totalTasks > 0 ? tasks.Average(t => t.Progress) : 0;
                    var overdueTasks = tasks.Count(t =>
                        t.DueDate < DateTime.Now && t.Status != "Completed"
                    );

                    progressReport.Add(
                        new
                        {
                            ProjectId = project.ProjectId,
                            ProjectName = project.Name,
                            Status = project.Status,
                            TotalTasks = totalTasks,
                            CompletedTasks = completedTasks,
                            AverageProgress = Math.Round(averageProgress, 2),
                            OverdueTasks = overdueTasks,
                            ProgressPercentage = totalTasks > 0
                                ? Math.Round((double)completedTasks / totalTasks * 100, 2)
                                : 0,
                            StartDate = project.StartDate,
                            EndDatePlanned = project.EndDatePlanned,
                            EndDateActual = project.EndDateActual,
                            IsOverdue = project.EndDatePlanned < DateTime.Now
                                && project.Status != "Completed",
                        }
                    );
                }

                var summary = new
                {
                    TotalProjects = projects.Count,
                    AverageProgress = progressReport.Count > 0
                        ? Math.Round(
                            progressReport.Cast<dynamic>().Average(p => p.ProgressPercentage),
                            2
                        )
                        : 0,
                    OverdueProjects = progressReport.Cast<dynamic>().Count(p => p.IsOverdue),
                    ProjectsWithOverdueTasks = progressReport
                        .Cast<dynamic>()
                        .Count(p => p.OverdueTasks > 0),
                    Projects = progressReport
                        .OrderByDescending(p => ((dynamic)p).ProgressPercentage)
                        .ToList(),
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving project progress report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get contractor ratings and performance metrics
        /// </summary>
        [HttpGet("contractors/ratings")]
        public async Task<IActionResult> GetContractorRatingsReport([FromQuery] int? limit = 10)
        {
            try
            {
                var usersCollection = _firestoreDb.Collection("users");
                var contractorsQuery = usersCollection.WhereEqualTo("role", "Contractor");
                var contractorsSnapshot = await contractorsQuery.GetSnapshotAsync();
                var contractors = contractorsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<User>())
                    .ToList();

                var contractorRatings = new List<object>();

                foreach (var contractor in contractors)
                {
                    // Get projects assigned to this contractor
                    var projectsQuery = _firestoreDb.Collection("projects");
                    var projectsSnapshot = await projectsQuery.GetSnapshotAsync();
                    var contractorProjects = projectsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Project>())
                        .Where(p => p.ProjectManagerId == contractor.UserId)
                        .ToList();

                    // Get quotations by this contractor
                    var quotationsQuery = _firestoreDb
                        .Collection("quotations")
                        .WhereEqualTo("contractorId", contractor.UserId);
                    var quotationsSnapshot = await quotationsQuery.GetSnapshotAsync();
                    var quotations = quotationsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Quotation>())
                        .ToList();

                    // Get invoices by this contractor
                    var invoicesQuery = _firestoreDb
                        .Collection("invoices")
                        .WhereEqualTo("contractorId", contractor.UserId);
                    var invoicesSnapshot = await invoicesQuery.GetSnapshotAsync();
                    var invoices = invoicesSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Invoice>())
                        .ToList();

                    var totalProjects = contractorProjects.Count;
                    var completedProjects = contractorProjects.Count(p => p.Status == "Completed");
                    var totalQuotations = quotations.Count;
                    var acceptedQuotations = quotations.Count(q => q.Status == "Approved");
                    var totalInvoices = invoices.Count;
                    var paidInvoices = invoices.Count(i => i.Status == "Paid");

                    var completionRate =
                        totalProjects > 0
                            ? Math.Round((double)completedProjects / totalProjects * 100, 2)
                            : 0;
                    var quotationAcceptanceRate =
                        totalQuotations > 0
                            ? Math.Round((double)acceptedQuotations / totalQuotations * 100, 2)
                            : 0;
                    var paymentRate =
                        totalInvoices > 0
                            ? Math.Round((double)paidInvoices / totalInvoices * 100, 2)
                            : 0;

                    // Calculate overall rating (weighted average)
                    var overallRating = (
                        completionRate * 0.4 + quotationAcceptanceRate * 0.3 + paymentRate * 0.3
                    );

                    contractorRatings.Add(
                        new
                        {
                            ContractorId = contractor.UserId,
                            ContractorName = contractor.FullName,
                            Email = contractor.Email,
                            TotalProjects = totalProjects,
                            CompletedProjects = completedProjects,
                            CompletionRate = completionRate,
                            TotalQuotations = totalQuotations,
                            AcceptedQuotations = acceptedQuotations,
                            QuotationAcceptanceRate = quotationAcceptanceRate,
                            TotalInvoices = totalInvoices,
                            PaidInvoices = paidInvoices,
                            PaymentRate = paymentRate,
                            OverallRating = Math.Round(overallRating, 2),
                            IsActive = contractor.IsActive,
                        }
                    );
                }

                var result = new
                {
                    TotalContractors = contractors.Count,
                    ActiveContractors = contractors.Count(c => c.IsActive),
                    TopPerformers = contractorRatings
                        .Cast<dynamic>()
                        .OrderByDescending(c => c.OverallRating)
                        .Take(limit ?? 10)
                        .ToList(),
                    AverageRating = contractorRatings.Count > 0
                        ? Math.Round(
                            contractorRatings.Cast<dynamic>().Average(c => c.OverallRating),
                            2
                        )
                        : 0,
                    AllContractors = contractorRatings
                        .OrderByDescending(c => ((dynamic)c).OverallRating)
                        .ToList(),
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error retrieving contractor ratings report",
                        error = ex.Message,
                    }
                );
            }
        }

        /// <summary>
        /// Get detailed contractor performance metrics
        /// </summary>
        [HttpGet("contractors/performance")]
        public async Task<IActionResult> GetContractorPerformanceReport(
            [FromQuery] string? contractorId = null,
            [FromQuery] int? year = null
        )
        {
            try
            {
                var usersCollection = _firestoreDb.Collection("users");
                var contractorsQuery = usersCollection.WhereEqualTo("role", "Contractor");

                if (!string.IsNullOrEmpty(contractorId))
                {
                    contractorsQuery = contractorsQuery.WhereEqualTo("userId", contractorId);
                }

                var contractorsSnapshot = await contractorsQuery.GetSnapshotAsync();
                var contractors = contractorsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<User>())
                    .ToList();

                var performanceReport = new List<object>();

                foreach (var contractor in contractors)
                {
                    // Get projects
                    var projectsQuery = _firestoreDb.Collection("projects");
                    var projectsSnapshot = await projectsQuery.GetSnapshotAsync();
                    var contractorProjects = projectsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Project>())
                        .Where(p => p.ProjectManagerId == contractor.UserId)
                        .ToList();

                    if (year.HasValue)
                    {
                        contractorProjects = contractorProjects
                            .Where(p => p.StartDate.Year == year.Value)
                            .ToList();
                    }

                    // Get tasks
                    var tasksQuery = _firestoreDb.Collection("projectTasks");
                    var tasksSnapshot = await tasksQuery.GetSnapshotAsync();
                    var contractorTasks = tasksSnapshot
                        .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                        .Where(t => contractorProjects.Any(p => p.ProjectId == t.ProjectId))
                        .ToList();

                    // Get quotations
                    var quotationsQuery = _firestoreDb
                        .Collection("quotations")
                        .WhereEqualTo("contractorId", contractor.UserId);
                    var quotationsSnapshot = await quotationsQuery.GetSnapshotAsync();
                    var quotations = quotationsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Quotation>())
                        .ToList();

                    if (year.HasValue)
                    {
                        quotations = quotations.Where(q => q.CreatedAt.Year == year.Value).ToList();
                    }

                    // Get invoices
                    var invoicesQuery = _firestoreDb
                        .Collection("invoices")
                        .WhereEqualTo("contractorId", contractor.UserId);
                    var invoicesSnapshot = await invoicesQuery.GetSnapshotAsync();
                    var invoices = invoicesSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Invoice>())
                        .ToList();

                    if (year.HasValue)
                    {
                        invoices = invoices.Where(i => i.IssuedDate.Year == year.Value).ToList();
                    }

                    // Calculate metrics
                    var totalRevenue = invoices.Sum(i => i.TotalAmount);
                    var paidRevenue = invoices
                        .Where(i => i.Status == "Paid")
                        .Sum(i => i.TotalAmount);
                    var pendingRevenue = invoices
                        .Where(i => i.Status == "Pending")
                        .Sum(i => i.TotalAmount);

                    var averageProjectDuration = contractorProjects.Any()
                        ? contractorProjects
                            .Where(p => p.EndDateActual.HasValue)
                            .Average(p => (p.EndDateActual!.Value - p.StartDate).TotalDays)
                        : 0;

                    var onTimeDeliveryRate = contractorProjects.Any()
                        ? contractorProjects
                            .Where(p => p.EndDateActual.HasValue)
                            .Count(p => p.EndDateActual <= p.EndDatePlanned)
                            * 100.0
                            / contractorProjects.Count(p => p.EndDateActual.HasValue)
                        : 0;

                    var taskCompletionRate = contractorTasks.Any()
                        ? contractorTasks.Count(t => t.Status == "Completed")
                            * 100.0
                            / contractorTasks.Count
                        : 0;

                    performanceReport.Add(
                        new
                        {
                            ContractorId = contractor.UserId,
                            ContractorName = contractor.FullName,
                            Email = contractor.Email,
                            Year = year ?? DateTime.Now.Year,
                            TotalProjects = contractorProjects.Count,
                            CompletedProjects = contractorProjects.Count(p =>
                                p.Status == "Completed"
                            ),
                            TotalTasks = contractorTasks.Count,
                            CompletedTasks = contractorTasks.Count(t => t.Status == "Completed"),
                            TaskCompletionRate = Math.Round(taskCompletionRate, 2),
                            TotalQuotations = quotations.Count,
                            AcceptedQuotations = quotations.Count(q => q.Status == "Approved"),
                            TotalInvoices = invoices.Count,
                            PaidInvoices = invoices.Count(i => i.Status == "Paid"),
                            TotalRevenue = totalRevenue,
                            PaidRevenue = paidRevenue,
                            PendingRevenue = pendingRevenue,
                            AverageProjectDuration = Math.Round(averageProjectDuration, 2),
                            OnTimeDeliveryRate = Math.Round(onTimeDeliveryRate, 2),
                            IsActive = contractor.IsActive,
                        }
                    );
                }

                return Ok(
                    new
                    {
                        Year = year ?? DateTime.Now.Year,
                        TotalContractors = contractors.Count,
                        PerformanceData = performanceReport
                            .OrderByDescending(p => ((dynamic)p).TotalRevenue)
                            .ToList(),
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error retrieving contractor performance report",
                        error = ex.Message,
                    }
                );
            }
        }

        /// <summary>
        /// Get financial summary for dashboard
        /// </summary>
        [HttpGet("financial/summary")]
        public async Task<IActionResult> GetFinancialSummaryReport(
            [FromQuery] int? year = null,
            [FromQuery] string? projectManagerId = null
        )
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;

                // Get invoices
                var invoicesQuery = _firestoreDb.Collection("invoices");
                var invoicesSnapshot = await invoicesQuery.GetSnapshotAsync();
                var invoices = invoicesSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Invoice>())
                    .ToList();

                if (year.HasValue)
                {
                    invoices = invoices.Where(i => i.IssuedDate.Year == targetYear).ToList();
                }

                // Get payments
                var paymentsQuery = _firestoreDb.Collection("payments");
                var paymentsSnapshot = await paymentsQuery.GetSnapshotAsync();
                var payments = paymentsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Payment>())
                    .ToList();

                if (year.HasValue)
                {
                    payments = payments.Where(p => p.PaymentDate.Year == targetYear).ToList();
                }

                // Get projects for budget analysis
                var projectsQuery = _firestoreDb.Collection("projects");
                if (!string.IsNullOrEmpty(projectManagerId))
                {
                    projectsQuery = projectsQuery.WhereEqualTo(
                        "projectManagerId",
                        projectManagerId
                    );
                }
                var projectsSnapshot = await projectsQuery.GetSnapshotAsync();
                var projects = projectsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Project>())
                    .ToList();

                if (year.HasValue)
                {
                    projects = projects.Where(p => p.StartDate.Year == targetYear).ToList();
                }

                var totalInvoiced = invoices.Sum(i => i.TotalAmount);
                var totalPaid = payments.Sum(p => p.Amount);
                var totalPending = invoices
                    .Where(i => i.Status == "Pending")
                    .Sum(i => i.TotalAmount);
                var totalOverdue = invoices
                    .Where(i => i.Status == "Overdue")
                    .Sum(i => i.TotalAmount);

                var totalPlannedBudget = projects.Sum(p => p.BudgetPlanned);
                var totalActualBudget = projects.Sum(p => p.BudgetActual);

                var financialSummary = new
                {
                    Year = targetYear,
                    Revenue = new
                    {
                        TotalInvoiced = totalInvoiced,
                        TotalPaid = totalPaid,
                        TotalPending = totalPending,
                        TotalOverdue = totalOverdue,
                        CollectionRate = totalInvoiced > 0
                            ? Math.Round(totalPaid / totalInvoiced * 100, 2)
                            : 0,
                    },
                    Budget = new
                    {
                        TotalPlanned = totalPlannedBudget,
                        TotalActual = totalActualBudget,
                        Variance = totalActualBudget - totalPlannedBudget,
                        VariancePercentage = totalPlannedBudget > 0
                            ? Math.Round(
                                (totalActualBudget - totalPlannedBudget) / totalPlannedBudget * 100,
                                2
                            )
                            : 0,
                    },
                    InvoiceStatus = new
                    {
                        Paid = invoices.Count(i => i.Status == "Paid"),
                        Pending = invoices.Count(i => i.Status == "Pending"),
                        Overdue = invoices.Count(i => i.Status == "Overdue"),
                        Total = invoices.Count,
                    },
                    TopProjectsByRevenue = projects
                        .Select(p => new
                        {
                            ProjectId = p.ProjectId,
                            ProjectName = p.Name,
                            Revenue = invoices
                                .Where(i => i.ProjectId == p.ProjectId)
                                .Sum(i => i.TotalAmount),
                            BudgetPlanned = p.BudgetPlanned,
                            BudgetActual = p.BudgetActual,
                        })
                        .OrderByDescending(p => p.Revenue)
                        .Take(10)
                        .ToList(),
                };

                return Ok(financialSummary);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error retrieving financial summary report",
                        error = ex.Message,
                    }
                );
            }
        }

        /// <summary>
        /// Get financial trends over time
        /// </summary>
        [HttpGet("financial/trends")]
        public async Task<IActionResult> GetFinancialTrendsReport([FromQuery] int months = 12)
        {
            try
            {
                var startDate = DateTime.Now.AddMonths(-months);

                // Get invoices
                var invoicesQuery = _firestoreDb.Collection("invoices");
                var invoicesSnapshot = await invoicesQuery.GetSnapshotAsync();
                var invoices = invoicesSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Invoice>())
                    .Where(i => i.IssuedDate >= startDate)
                    .ToList();

                // Get payments
                var paymentsQuery = _firestoreDb.Collection("payments");
                var paymentsSnapshot = await paymentsQuery.GetSnapshotAsync();
                var payments = paymentsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Payment>())
                    .Where(p => p.PaymentDate >= startDate)
                    .ToList();

                // Group by month
                var monthlyTrends = new List<object>();
                for (int i = months - 1; i >= 0; i--)
                {
                    var monthStart = DateTime.Now.AddMonths(-i).Date;
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthInvoices = invoices
                        .Where(i => i.IssuedDate >= monthStart && i.IssuedDate <= monthEnd)
                        .ToList();
                    var monthPayments = payments
                        .Where(p => p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                        .ToList();

                    monthlyTrends.Add(
                        new
                        {
                            Month = monthStart.ToString("yyyy-MM"),
                            MonthName = monthStart.ToString("MMMM yyyy"),
                            Invoiced = monthInvoices.Sum(i => i.TotalAmount),
                            Paid = monthPayments.Sum(p => p.Amount),
                            InvoiceCount = monthInvoices.Count,
                            PaymentCount = monthPayments.Count,
                        }
                    );
                }

                var trends = new
                {
                    Period = $"{months} months",
                    StartDate = startDate,
                    EndDate = DateTime.Now,
                    MonthlyTrends = monthlyTrends,
                    TotalInvoiced = invoices.Sum(i => i.TotalAmount),
                    TotalPaid = payments.Sum(p => p.Amount),
                    AverageMonthlyRevenue = monthlyTrends.Count > 0
                        ? Math.Round(monthlyTrends.Cast<dynamic>().Average(m => m.Paid), 2)
                        : 0,
                    GrowthRate = CalculateGrowthRate(monthlyTrends.Cast<dynamic>().ToList()),
                };

                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving financial trends report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get maintenance summary report
        /// </summary>
        [HttpGet("maintenance/summary")]
        public async Task<IActionResult> GetMaintenanceSummaryReport(
            [FromQuery] int? year = null,
            [FromQuery] string? priority = null
        )
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;

                var maintenanceQuery = _firestoreDb.Collection("maintenanceRequests");
                var maintenanceSnapshot = await maintenanceQuery.GetSnapshotAsync();
                var maintenanceRequests = maintenanceSnapshot
                    .Documents.Select(doc => doc.ConvertTo<MaintenanceRequest>())
                    .Where(m => m.CreatedAt.Year == targetYear)
                    .ToList();

                if (!string.IsNullOrEmpty(priority))
                {
                    maintenanceRequests = maintenanceRequests
                        .Where(m => m.Priority == priority)
                        .ToList();
                }

                var maintenanceSummary = new
                {
                    Year = targetYear,
                    TotalRequests = maintenanceRequests.Count,
                    StatusDistribution = maintenanceRequests
                        .GroupBy(m => m.Status)
                        .Select(g => new
                        {
                            Status = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round(
                                (double)g.Count() / maintenanceRequests.Count * 100,
                                2
                            ),
                        })
                        .ToList(),
                    PriorityDistribution = maintenanceRequests
                        .GroupBy(m => m.Priority)
                        .Select(g => new
                        {
                            Priority = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round(
                                (double)g.Count() / maintenanceRequests.Count * 100,
                                2
                            ),
                        })
                        .ToList(),
                    AverageResolutionTime = maintenanceRequests
                        .Where(m => m.ResolvedAt.HasValue)
                        .Select(m => (m.ResolvedAt!.Value - m.CreatedAt).TotalDays)
                        .DefaultIfEmpty(0)
                        .Average(),
                    ResolvedRequests = maintenanceRequests.Count(m => m.Status == "Resolved"),
                    PendingRequests = maintenanceRequests.Count(m => m.Status == "Pending"),
                    OverdueRequests = maintenanceRequests.Count(m =>
                        m.CreatedAt.AddDays(7) < DateTime.Now && m.Status != "Resolved"
                    ),
                    RequestsByMonth = maintenanceRequests
                        .GroupBy(m => m.CreatedAt.Month)
                        .Select(g => new
                        {
                            Month = g.Key,
                            MonthName = new DateTime(targetYear, g.Key, 1).ToString("MMMM"),
                            Count = g.Count(),
                        })
                        .OrderBy(m => m.Month)
                        .ToList(),
                };

                return Ok(maintenanceSummary);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error retrieving maintenance summary report",
                        error = ex.Message,
                    }
                );
            }
        }

        /// <summary>
        /// Get task performance metrics
        /// </summary>
        [HttpGet("tasks/performance")]
        public async Task<IActionResult> GetTaskPerformanceReport(
            [FromQuery] string? projectId = null,
            [FromQuery] string? assignedTo = null
        )
        {
            try
            {
                var tasksQuery = _firestoreDb.Collection("projectTasks");

                if (!string.IsNullOrEmpty(projectId))
                {
                    tasksQuery = tasksQuery.WhereEqualTo("projectId", projectId);
                }

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    tasksQuery = tasksQuery.WhereEqualTo("assignedTo", assignedTo);
                }

                var tasksSnapshot = await tasksQuery.GetSnapshotAsync();
                var tasks = tasksSnapshot
                    .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                    .ToList();

                var performanceMetrics = new
                {
                    TotalTasks = tasks.Count,
                    CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                    InProgressTasks = tasks.Count(t => t.Status == "In Progress"),
                    PendingTasks = tasks.Count(t => t.Status == "Pending"),
                    OverdueTasks = tasks.Count(t =>
                        t.DueDate < DateTime.Now && t.Status != "Completed"
                    ),
                    AverageProgress = tasks.Any()
                        ? Math.Round(tasks.Average(t => t.Progress), 2)
                        : 0,
                    AverageCompletionTime = tasks
                        .Where(t => t.CompletedDate.HasValue)
                        .Select(t => (t.CompletedDate!.Value - t.StartDate).TotalDays)
                        .DefaultIfEmpty(0)
                        .Average(),
                    TasksByPriority = tasks
                        .GroupBy(t => t.Priority)
                        .Select(g => new
                        {
                            Priority = g.Key,
                            Count = g.Count(),
                            Completed = g.Count(t => t.Status == "Completed"),
                            CompletionRate = Math.Round(
                                (double)g.Count(t => t.Status == "Completed") / g.Count() * 100,
                                2
                            ),
                        })
                        .ToList(),
                    TasksByStatus = tasks
                        .GroupBy(t => t.Status)
                        .Select(g => new
                        {
                            Status = g.Key,
                            Count = g.Count(),
                            Percentage = Math.Round((double)g.Count() / tasks.Count * 100, 2),
                        })
                        .ToList(),
                    TopPerformers = tasks
                        .Where(t => !string.IsNullOrEmpty(t.AssignedTo))
                        .GroupBy(t => t.AssignedTo)
                        .Select(g => new
                        {
                            AssignedTo = g.Key,
                            TotalTasks = g.Count(),
                            CompletedTasks = g.Count(t => t.Status == "Completed"),
                            CompletionRate = Math.Round(
                                (double)g.Count(t => t.Status == "Completed") / g.Count() * 100,
                                2
                            ),
                            AverageProgress = Math.Round(g.Average(t => t.Progress), 2),
                        })
                        .OrderByDescending(p => p.CompletionRate)
                        .Take(10)
                        .ToList(),
                };

                return Ok(performanceMetrics);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving task performance report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get AI-powered risk analysis report
        /// </summary>
        [HttpGet("ai/risk-analysis")]
        public async Task<IActionResult> GetRiskAnalysisReport([FromQuery] string? projectId = null)
        {
            try
            {
                var projectsQuery = _firestoreDb.Collection("projects");
                if (!string.IsNullOrEmpty(projectId))
                {
                    projectsQuery = projectsQuery.WhereEqualTo("projectId", projectId);
                }

                var projectsSnapshot = await projectsQuery.GetSnapshotAsync();
                var projects = projectsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Project>())
                    .ToList();

                var riskAnalysis = new List<object>();

                foreach (var project in projects)
                {
                    // Get project tasks
                    var tasksQuery = _firestoreDb
                        .Collection("projectTasks")
                        .WhereEqualTo("projectId", project.ProjectId);
                    var tasksSnapshot = await tasksQuery.GetSnapshotAsync();
                    var tasks = tasksSnapshot
                        .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                        .ToList();

                    // Get project invoices
                    var invoicesQuery = _firestoreDb
                        .Collection("invoices")
                        .WhereEqualTo("projectId", project.ProjectId);
                    var invoicesSnapshot = await invoicesQuery.GetSnapshotAsync();
                    var invoices = invoicesSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Invoice>())
                        .ToList();

                    // Calculate risk factors
                    var budgetVariance =
                        project.BudgetPlanned > 0
                            ? (project.BudgetActual - project.BudgetPlanned)
                                / project.BudgetPlanned
                                * 100
                            : 0;

                    var scheduleVariance =
                        project.EndDatePlanned > project.StartDate
                            ? (DateTime.Now - project.StartDate).TotalDays
                                / (project.EndDatePlanned - project.StartDate).TotalDays
                                * 100
                            : 0;

                    var overdueTasks = tasks.Count(t =>
                        t.DueDate < DateTime.Now && t.Status != "Completed"
                    );
                    var overdueInvoices = invoices.Count(i =>
                        i.DueDate < DateTime.Now && i.Status != "Paid"
                    );

                    var riskScore = CalculateRiskScore(
                        budgetVariance,
                        scheduleVariance,
                        overdueTasks,
                        overdueInvoices,
                        tasks.Count
                    );

                    riskAnalysis.Add(
                        new
                        {
                            ProjectId = project.ProjectId,
                            ProjectName = project.Name,
                            Status = project.Status,
                            RiskScore = riskScore,
                            RiskLevel = GetRiskLevel(riskScore),
                            BudgetVariance = Math.Round(budgetVariance, 2),
                            ScheduleVariance = Math.Round(scheduleVariance, 2),
                            OverdueTasks = overdueTasks,
                            OverdueInvoices = overdueInvoices,
                            TotalTasks = tasks.Count,
                            TotalInvoices = invoices.Count,
                            Recommendations = GetRiskRecommendations(
                                riskScore,
                                budgetVariance,
                                scheduleVariance,
                                overdueTasks,
                                overdueInvoices
                            ),
                        }
                    );
                }

                var summary = new
                {
                    TotalProjects = projects.Count,
                    HighRiskProjects = riskAnalysis
                        .Cast<dynamic>()
                        .Count(p => p.RiskLevel == "High"),
                    MediumRiskProjects = riskAnalysis
                        .Cast<dynamic>()
                        .Count(p => p.RiskLevel == "Medium"),
                    LowRiskProjects = riskAnalysis.Cast<dynamic>().Count(p => p.RiskLevel == "Low"),
                    AverageRiskScore = riskAnalysis.Cast<dynamic>().Any()
                        ? Math.Round(riskAnalysis.Cast<dynamic>().Average(p => p.RiskScore), 2)
                        : 0,
                    RiskAnalysis = riskAnalysis
                        .OrderByDescending(r => ((dynamic)r).RiskScore)
                        .ToList(),
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving risk analysis report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get AI-powered maintenance forecast
        /// </summary>
        [HttpGet("ai/maintenance-forecast")]
        public async Task<IActionResult> GetMaintenanceForecastReport([FromQuery] int months = 6)
        {
            try
            {
                var maintenanceQuery = _firestoreDb.Collection("maintenanceRequests");
                var maintenanceSnapshot = await maintenanceQuery.GetSnapshotAsync();
                var maintenanceRequests = maintenanceSnapshot
                    .Documents.Select(doc => doc.ConvertTo<MaintenanceRequest>())
                    .ToList();

                // Analyze historical patterns
                var historicalPatterns = maintenanceRequests
                    .GroupBy(m => m.Priority)
                    .Select(g => new
                    {
                        Priority = g.Key,
                        Count = g.Count(),
                        AverageResolutionTime = g.Where(m => m.ResolvedAt.HasValue)
                            .Select(m => (m.ResolvedAt!.Value - m.CreatedAt).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average(),
                    })
                    .ToList();

                // Generate forecast for next months
                var forecast = new List<object>();
                var currentDate = DateTime.Now;

                for (int i = 1; i <= months; i++)
                {
                    var forecastDate = currentDate.AddMonths(i);
                    var historicalMonth = maintenanceRequests
                        .Where(m => m.CreatedAt.Month == forecastDate.Month)
                        .ToList();

                    var predictedRequests = Math.Round(historicalMonth.Count * 1.1); // 10% growth assumption
                    var predictedByPriority = historicalPatterns
                        .Select(p => new
                        {
                            Priority = p.Priority,
                            PredictedCount = Math.Round(
                                predictedRequests * (p.Count / (double)maintenanceRequests.Count)
                            ),
                        })
                        .ToList();

                    forecast.Add(
                        new
                        {
                            Month = forecastDate.ToString("yyyy-MM"),
                            MonthName = forecastDate.ToString("MMMM yyyy"),
                            PredictedRequests = predictedRequests,
                            PredictedByPriority = predictedByPriority,
                            Confidence = CalculateForecastConfidence(
                                historicalMonth.Count,
                                maintenanceRequests.Count
                            ),
                        }
                    );
                }

                var forecastReport = new
                {
                    ForecastPeriod = $"{months} months",
                    HistoricalData = new
                    {
                        TotalRequests = maintenanceRequests.Count,
                        AverageMonthlyRequests = maintenanceRequests.Count / 12.0,
                        Patterns = historicalPatterns,
                    },
                    Forecast = forecast,
                    Recommendations = new[]
                    {
                        "Increase maintenance staff during peak months",
                        "Implement predictive maintenance for high-priority equipment",
                        "Consider preventive maintenance scheduling",
                        "Monitor seasonal patterns for resource planning",
                    },
                };

                return Ok(forecastReport);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Error retrieving maintenance forecast report",
                        error = ex.Message,
                    }
                );
            }
        }

        /// <summary>
        /// Get key performance indicators (KPIs)
        /// </summary>
        [HttpGet("kpis")]
        public async Task<IActionResult> GetKPIsReport([FromQuery] int? year = null)
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;

                // Get all data
                var projectsSnapshot = await _firestoreDb.Collection("projects").GetSnapshotAsync();
                var projects = projectsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Project>())
                    .ToList();

                var tasksSnapshot = await _firestoreDb
                    .Collection("projectTasks")
                    .GetSnapshotAsync();
                var tasks = tasksSnapshot
                    .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                    .ToList();

                var invoicesSnapshot = await _firestoreDb.Collection("invoices").GetSnapshotAsync();
                var invoices = invoicesSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Invoice>())
                    .ToList();

                var paymentsSnapshot = await _firestoreDb.Collection("payments").GetSnapshotAsync();
                var payments = paymentsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Payment>())
                    .ToList();

                var maintenanceSnapshot = await _firestoreDb
                    .Collection("maintenanceRequests")
                    .GetSnapshotAsync();
                var maintenanceRequests = maintenanceSnapshot
                    .Documents.Select(doc => doc.ConvertTo<MaintenanceRequest>())
                    .ToList();

                // Filter by year if specified
                if (year.HasValue)
                {
                    projects = projects.Where(p => p.StartDate.Year == targetYear).ToList();
                    tasks = tasks.Where(t => t.StartDate.Year == targetYear).ToList();
                    invoices = invoices.Where(i => i.IssuedDate.Year == targetYear).ToList();
                    payments = payments.Where(p => p.PaymentDate.Year == targetYear).ToList();
                    maintenanceRequests = maintenanceRequests
                        .Where(m => m.CreatedAt.Year == targetYear)
                        .ToList();
                }

                var kpis = new
                {
                    Year = targetYear,
                    ProjectKPIs = new
                    {
                        TotalProjects = projects.Count,
                        CompletedProjects = projects.Count(p => p.Status == "Completed"),
                        ProjectCompletionRate = projects.Count > 0
                            ? Math.Round(
                                (double)projects.Count(p => p.Status == "Completed")
                                    / projects.Count
                                    * 100,
                                2
                            )
                            : 0,
                        OnTimeDeliveryRate = projects.Count(p => p.EndDateActual.HasValue) > 0
                            ? Math.Round(
                                (double)projects.Count(p => p.EndDateActual <= p.EndDatePlanned)
                                    / projects.Count(p => p.EndDateActual.HasValue)
                                    * 100,
                                2
                            )
                            : 0,
                        AverageProjectDuration = projects
                            .Where(p => p.EndDateActual.HasValue)
                            .Select(p => (p.EndDateActual!.Value - p.StartDate).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average(),
                    },
                    FinancialKPIs = new
                    {
                        TotalRevenue = invoices.Sum(i => i.TotalAmount),
                        TotalPaid = payments.Sum(p => p.Amount),
                        CollectionRate = invoices.Sum(i => i.TotalAmount) > 0
                            ? Math.Round(
                                payments.Sum(p => p.Amount)
                                    / invoices.Sum(i => i.TotalAmount)
                                    * 100,
                                2
                            )
                            : 0,
                        AverageInvoiceValue = invoices.Count > 0
                            ? Math.Round(invoices.Average(i => i.TotalAmount), 2)
                            : 0,
                        OutstandingAmount = invoices
                            .Where(i => i.Status != "Paid")
                            .Sum(i => i.TotalAmount),
                    },
                    TaskKPIs = new
                    {
                        TotalTasks = tasks.Count,
                        CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                        TaskCompletionRate = tasks.Count > 0
                            ? Math.Round(
                                (double)tasks.Count(t => t.Status == "Completed")
                                    / tasks.Count
                                    * 100,
                                2
                            )
                            : 0,
                        OverdueTasks = tasks.Count(t =>
                            t.DueDate < DateTime.Now && t.Status != "Completed"
                        ),
                        AverageTaskDuration = tasks
                            .Where(t => t.CompletedDate.HasValue)
                            .Select(t => (t.CompletedDate!.Value - t.StartDate).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average(),
                    },
                    MaintenanceKPIs = new
                    {
                        TotalRequests = maintenanceRequests.Count,
                        ResolvedRequests = maintenanceRequests.Count(m => m.Status == "Resolved"),
                        ResolutionRate = maintenanceRequests.Count > 0
                            ? Math.Round(
                                (double)maintenanceRequests.Count(m => m.Status == "Resolved")
                                    / maintenanceRequests.Count
                                    * 100,
                                2
                            )
                            : 0,
                        AverageResolutionTime = maintenanceRequests
                            .Where(m => m.ResolvedAt.HasValue)
                            .Select(m => (m.ResolvedAt!.Value - m.CreatedAt).TotalDays)
                            .DefaultIfEmpty(0)
                            .Average(),
                        OverdueRequests = maintenanceRequests.Count(m =>
                            m.CreatedAt.AddDays(7) < DateTime.Now && m.Status != "Resolved"
                        ),
                    },
                    QualityKPIs = new
                    {
                        BudgetVariance = projects.Sum(p => p.BudgetActual)
                            - projects.Sum(p => p.BudgetPlanned),
                        BudgetVariancePercentage = projects.Sum(p => p.BudgetPlanned) > 0
                            ? Math.Round(
                                (
                                    projects.Sum(p => p.BudgetActual)
                                    - projects.Sum(p => p.BudgetPlanned)
                                )
                                    / projects.Sum(p => p.BudgetPlanned)
                                    * 100,
                                2
                            )
                            : 0,
                        CustomerSatisfaction = CalculateCustomerSatisfaction(
                            projects,
                            invoices,
                            maintenanceRequests
                        ),
                        ReworkRate = CalculateReworkRate(tasks),
                    },
                };

                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving KPIs report", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Export reports data in various formats
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportReports(
            [FromQuery] string format = "json",
            [FromQuery] string reportType = "all",
            [FromQuery] int? year = null
        )
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;
                var exportData = new Dictionary<string, object>();

                // Get all reports based on type
                if (reportType == "all" || reportType == "projects")
                {
                    var projectsSnapshot = await _firestoreDb
                        .Collection("projects")
                        .GetSnapshotAsync();
                    var projects = projectsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Project>())
                        .ToList();
                    if (year.HasValue)
                    {
                        projects = projects.Where(p => p.StartDate.Year == targetYear).ToList();
                    }
                    exportData["projects"] = projects;
                }

                if (reportType == "all" || reportType == "financial")
                {
                    var invoicesSnapshot = await _firestoreDb
                        .Collection("invoices")
                        .GetSnapshotAsync();
                    var invoices = invoicesSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Invoice>())
                        .ToList();
                    if (year.HasValue)
                    {
                        invoices = invoices.Where(i => i.IssuedDate.Year == targetYear).ToList();
                    }
                    exportData["invoices"] = invoices;

                    var paymentsSnapshot = await _firestoreDb
                        .Collection("payments")
                        .GetSnapshotAsync();
                    var payments = paymentsSnapshot
                        .Documents.Select(doc => doc.ConvertTo<Payment>())
                        .ToList();
                    if (year.HasValue)
                    {
                        payments = payments.Where(p => p.PaymentDate.Year == targetYear).ToList();
                    }
                    exportData["payments"] = payments;
                }

                if (reportType == "all" || reportType == "maintenance")
                {
                    var maintenanceSnapshot = await _firestoreDb
                        .Collection("maintenanceRequests")
                        .GetSnapshotAsync();
                    var maintenanceRequests = maintenanceSnapshot
                        .Documents.Select(doc => doc.ConvertTo<MaintenanceRequest>())
                        .ToList();
                    if (year.HasValue)
                    {
                        maintenanceRequests = maintenanceRequests
                            .Where(m => m.CreatedAt.Year == targetYear)
                            .ToList();
                    }
                    exportData["maintenanceRequests"] = maintenanceRequests;
                }

                if (reportType == "all" || reportType == "tasks")
                {
                    var tasksSnapshot = await _firestoreDb
                        .Collection("projectTasks")
                        .GetSnapshotAsync();
                    var tasks = tasksSnapshot
                        .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                        .ToList();
                    if (year.HasValue)
                    {
                        tasks = tasks.Where(t => t.StartDate.Year == targetYear).ToList();
                    }
                    exportData["tasks"] = tasks;
                }

                // Add metadata
                exportData["exportMetadata"] = new
                {
                    ExportDate = DateTime.Now,
                    Year = targetYear,
                    ReportType = reportType,
                    Format = format,
                    RecordCount = exportData.Values.Sum(v => ((IEnumerable<object>)v).Count()),
                };

                if (format.ToLower() == "csv")
                {
                    // For CSV export, you would need to implement CSV conversion
                    return BadRequest(
                        new { message = "CSV export not yet implemented. Please use JSON format." }
                    );
                }

                return Ok(exportData);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error exporting reports", error = ex.Message }
                );
            }
        }

        /// <summary>
        /// Get comprehensive overview dashboard data
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewReport([FromQuery] int? year = null)
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;

                // Get all data
                var projectsSnapshot = await _firestoreDb.Collection("projects").GetSnapshotAsync();
                var projects = projectsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Project>())
                    .ToList();

                var tasksSnapshot = await _firestoreDb
                    .Collection("projectTasks")
                    .GetSnapshotAsync();
                var tasks = tasksSnapshot
                    .Documents.Select(doc => doc.ConvertTo<ProjectTask>())
                    .ToList();

                var invoicesSnapshot = await _firestoreDb.Collection("invoices").GetSnapshotAsync();
                var invoices = invoicesSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Invoice>())
                    .ToList();

                var paymentsSnapshot = await _firestoreDb.Collection("payments").GetSnapshotAsync();
                var payments = paymentsSnapshot
                    .Documents.Select(doc => doc.ConvertTo<Payment>())
                    .ToList();

                var maintenanceSnapshot = await _firestoreDb
                    .Collection("maintenanceRequests")
                    .GetSnapshotAsync();
                var maintenanceRequests = maintenanceSnapshot
                    .Documents.Select(doc => doc.ConvertTo<MaintenanceRequest>())
                    .ToList();

                var usersSnapshot = await _firestoreDb.Collection("users").GetSnapshotAsync();
                var users = usersSnapshot.Documents.Select(doc => doc.ConvertTo<User>()).ToList();

                // Filter by year if specified
                if (year.HasValue)
                {
                    projects = projects.Where(p => p.StartDate.Year == targetYear).ToList();
                    tasks = tasks.Where(t => t.StartDate.Year == targetYear).ToList();
                    invoices = invoices.Where(i => i.IssuedDate.Year == targetYear).ToList();
                    payments = payments.Where(p => p.PaymentDate.Year == targetYear).ToList();
                    maintenanceRequests = maintenanceRequests
                        .Where(m => m.CreatedAt.Year == targetYear)
                        .ToList();
                }

                var overview = new
                {
                    Year = targetYear,
                    Summary = new
                    {
                        TotalProjects = projects.Count,
                        ActiveProjects = projects.Count(p => p.Status == "In Progress"),
                        CompletedProjects = projects.Count(p => p.Status == "Completed"),
                        TotalTasks = tasks.Count,
                        CompletedTasks = tasks.Count(t => t.Status == "Completed"),
                        TotalUsers = users.Count,
                        ActiveUsers = users.Count(u => u.IsActive),
                        TotalRevenue = invoices.Sum(i => i.TotalAmount),
                        TotalPaid = payments.Sum(p => p.Amount),
                        TotalMaintenanceRequests = maintenanceRequests.Count,
                        ResolvedMaintenanceRequests = maintenanceRequests.Count(m =>
                            m.Status == "Resolved"
                        ),
                    },
                    QuickStats = new
                    {
                        ProjectCompletionRate = projects.Count > 0
                            ? Math.Round(
                                (double)projects.Count(p => p.Status == "Completed")
                                    / projects.Count
                                    * 100,
                                2
                            )
                            : 0,
                        TaskCompletionRate = tasks.Count > 0
                            ? Math.Round(
                                (double)tasks.Count(t => t.Status == "Completed")
                                    / tasks.Count
                                    * 100,
                                2
                            )
                            : 0,
                        CollectionRate = invoices.Sum(i => i.TotalAmount) > 0
                            ? Math.Round(
                                payments.Sum(p => p.Amount)
                                    / invoices.Sum(i => i.TotalAmount)
                                    * 100,
                                2
                            )
                            : 0,
                        MaintenanceResolutionRate = maintenanceRequests.Count > 0
                            ? Math.Round(
                                (double)maintenanceRequests.Count(m => m.Status == "Resolved")
                                    / maintenanceRequests.Count
                                    * 100,
                                2
                            )
                            : 0,
                    },
                    RecentActivity = new
                    {
                        RecentProjects = projects
                            .OrderByDescending(p => p.StartDate)
                            .Take(5)
                            .Select(p => new
                            {
                                p.ProjectId,
                                p.Name,
                                p.Status,
                                p.StartDate,
                            })
                            .ToList(),
                        RecentTasks = tasks
                            .OrderByDescending(t => t.StartDate)
                            .Take(5)
                            .Select(t => new
                            {
                                t.TaskId,
                                t.Name,
                                t.Status,
                                t.StartDate,
                            })
                            .ToList(),
                        RecentInvoices = invoices
                            .OrderByDescending(i => i.IssuedDate)
                            .Take(5)
                            .Select(i => new
                            {
                                i.InvoiceId,
                                i.InvoiceNumber,
                                i.TotalAmount,
                                i.Status,
                                i.IssuedDate,
                            })
                            .ToList(),
                    },
                    Alerts = new
                    {
                        OverdueProjects = projects.Count(p =>
                            p.EndDatePlanned < DateTime.Now && p.Status != "Completed"
                        ),
                        OverdueTasks = tasks.Count(t =>
                            t.DueDate < DateTime.Now && t.Status != "Completed"
                        ),
                        OverdueInvoices = invoices.Count(i =>
                            i.DueDate < DateTime.Now && i.Status != "Paid"
                        ),
                        OverdueMaintenanceRequests = maintenanceRequests.Count(m =>
                            m.CreatedAt.AddDays(7) < DateTime.Now && m.Status != "Resolved"
                        ),
                    },
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Error retrieving overview report", error = ex.Message }
                );
            }
        }

        // Helper methods
        private double CalculateGrowthRate(List<dynamic> monthlyTrends)
        {
            if (monthlyTrends.Count < 2)
                return 0;

            var firstMonth = monthlyTrends.First().Paid;
            var lastMonth = monthlyTrends.Last().Paid;

            if (firstMonth == 0)
                return 0;

            return Math.Round((lastMonth - firstMonth) / firstMonth * 100, 2);
        }

        private double CalculateRiskScore(
            double budgetVariance,
            double scheduleVariance,
            int overdueTasks,
            int overdueInvoices,
            int totalTasks
        )
        {
            var budgetRisk = Math.Min(Math.Abs(budgetVariance) / 10, 10); // Max 10 points
            var scheduleRisk = Math.Min(scheduleVariance / 10, 10); // Max 10 points
            var taskRisk = totalTasks > 0 ? (double)overdueTasks / totalTasks * 10 : 0; // Max 10 points
            var invoiceRisk = Math.Min(overdueInvoices * 2, 10); // Max 10 points

            return Math.Round(budgetRisk + scheduleRisk + taskRisk + invoiceRisk, 2);
        }

        private string GetRiskLevel(double riskScore)
        {
            if (riskScore >= 7)
                return "High";
            if (riskScore >= 4)
                return "Medium";
            return "Low";
        }

        private List<string> GetRiskRecommendations(
            double riskScore,
            double budgetVariance,
            double scheduleVariance,
            int overdueTasks,
            int overdueInvoices
        )
        {
            var recommendations = new List<string>();

            if (budgetVariance > 10)
                recommendations.Add("Review project budget and consider cost reduction measures");

            if (scheduleVariance > 20)
                recommendations.Add("Accelerate project timeline or adjust expectations");

            if (overdueTasks > 0)
                recommendations.Add("Prioritize and complete overdue tasks immediately");

            if (overdueInvoices > 0)
                recommendations.Add("Follow up on overdue payments to improve cash flow");

            if (riskScore >= 7)
                recommendations.Add("Schedule emergency project review meeting");

            return recommendations;
        }

        private double CalculateForecastConfidence(int historicalCount, int totalCount)
        {
            if (totalCount == 0)
                return 0;
            var confidence = Math.Min(historicalCount / (double)totalCount * 100, 95);
            return Math.Round(confidence, 2);
        }

        private double CalculateCustomerSatisfaction(
            List<Project> projects,
            List<Invoice> invoices,
            List<MaintenanceRequest> maintenanceRequests
        )
        {
            // Simplified customer satisfaction calculation
            var onTimeProjects = projects.Count(p => p.EndDateActual <= p.EndDatePlanned);
            var totalProjects = projects.Count(p => p.EndDateActual.HasValue);
            var paidInvoices = invoices.Count(i => i.Status == "Paid");
            var totalInvoices = invoices.Count;
            var resolvedMaintenance = maintenanceRequests.Count(m => m.Status == "Resolved");
            var totalMaintenance = maintenanceRequests.Count;

            var projectSatisfaction =
                totalProjects > 0 ? (double)onTimeProjects / totalProjects : 1;
            var paymentSatisfaction = totalInvoices > 0 ? (double)paidInvoices / totalInvoices : 1;
            var maintenanceSatisfaction =
                totalMaintenance > 0 ? (double)resolvedMaintenance / totalMaintenance : 1;

            return Math.Round(
                (projectSatisfaction + paymentSatisfaction + maintenanceSatisfaction) / 3 * 100,
                2
            );
        }

        private double CalculateReworkRate(List<ProjectTask> tasks)
        {
            // Simplified rework rate calculation based on task status changes
            var reworkedTasks = tasks.Count(t => t.Status == "In Progress" && t.Progress < 50);
            var totalTasks = tasks.Count;

            return totalTasks > 0 ? Math.Round((double)reworkedTasks / totalTasks * 100, 2) : 0;
        }
    }
}
