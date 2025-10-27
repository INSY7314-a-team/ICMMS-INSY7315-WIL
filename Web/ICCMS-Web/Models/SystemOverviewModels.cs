using System;
using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    // View Models for System Overview Dashboard
    public class SystemOverviewViewModel
    {
        public SystemHealth SystemHealth { get; set; } = new();
        public UserStatistics UserStatistics { get; set; } = new();
        public ProjectStatistics ProjectStatistics { get; set; } = new();
        public FinancialStatistics FinancialStatistics { get; set; } = new();
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
        public List<RecentActivity> RecentActivity { get; set; } = new();
        public List<SystemAlert> SystemAlerts { get; set; } = new();
        public DatabaseHealth DatabaseHealth { get; set; } = new();
        public ApiStatus ApiStatus { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class SystemHealth
    {
        public string OverallStatus { get; set; } = "";
        public string Uptime { get; set; } = "";
        public DateTime LastHealthCheck { get; set; }
        public string SystemLoad { get; set; } = "";
        public string MemoryUsage { get; set; } = "";
        public string DiskUsage { get; set; } = "";
        public string CpuUsage { get; set; } = "";
    }

    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int ClientUsers { get; set; }
        public int ContractorUsers { get; set; }
        public int ManagerUsers { get; set; }
        public int NewUsersThisWeek { get; set; }
        public DateTime LastLoginActivity { get; set; }
    }

    public class ProjectStatistics
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int DraftProjects { get; set; }
        public int ProjectsThisMonth { get; set; }
        public string AverageProjectDuration { get; set; } = "";
        public string MostActiveProject { get; set; } = "";
    }

    public class FinancialStatistics
    {
        public int TotalQuotations { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalEstimates { get; set; }
        public int PendingQuotations { get; set; }
        public int PaidInvoices { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal AverageInvoiceValue { get; set; }
    }

    public class PerformanceMetrics
    {
        public string AverageResponseTime { get; set; } = "";
        public int RequestsPerSecond { get; set; }
        public double ErrorRate { get; set; }
        public string DatabaseResponseTime { get; set; } = "";
        public string ApiUptime { get; set; } = "";
        public DateTime LastPerformanceCheck { get; set; }
        public int PeakConcurrentUsers { get; set; }
        public string AverageSessionDuration { get; set; } = "";
    }

    public class RecentActivity
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class SystemAlert
    {
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; } = "";
    }

    public class DatabaseHealth
    {
        public string Status { get; set; } = "";
        public string ResponseTime { get; set; } = "";
        public DateTime LastBackup { get; set; }
        public string ConnectionPool { get; set; } = "";
        public string QueryPerformance { get; set; } = "";
        public string StorageUsed { get; set; } = "";
        public DateTime LastHealthCheck { get; set; }
    }

    public class ApiStatus
    {
        public string OverallStatus { get; set; } = "";
        public int HealthyEndpoints { get; set; }
        public int TotalEndpoints { get; set; }
        public string AverageResponseTime { get; set; } = "";
        public List<EndpointStatus> EndpointStatuses { get; set; } = new();
        public DateTime LastHealthCheck { get; set; }
    }

    public class EndpointStatus
    {
        public string Endpoint { get; set; } = "";
        public string Status { get; set; } = "";
        public string ResponseTime { get; set; } = "";
        public DateTime LastChecked { get; set; }
    }
}
