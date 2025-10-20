using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Centralized status-to-color and label mapping for Projects.
    /// Prevents scattered string literals and ensures UI consistency.
    /// </summary>
    public static class ProjectStatusHelper
    {
        private static readonly Dictionary<string, (string Label, string ColorClass)> StatusMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Draft", ("Draft", "bg-secondary") },
                { "PendingPMApproval", ("Pending PM Approval", "bg-warning text-dark") },
                { "SentToClient", ("Sent to Client", "bg-info text-dark") },
                { "ClientAccepted", ("Client Accepted", "bg-success") },
                { "ClientDeclined", ("Client Declined", "bg-danger") },
                { "Active", ("Active", "bg-primary") },
                { "Completed", ("Completed", "bg-success") },
                { "Maintenance", ("Maintenance", "bg-dark") },
                { "PMRejected", ("PM Rejected", "bg-danger") }
            };

        /// <summary>
        /// Returns a human-readable label for a given project status.
        /// </summary>
        public static string GetLabel(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "Unknown";
            return StatusMap.TryGetValue(status, out var val) ? val.Label : status;
        }

        /// <summary>
        /// Returns a Bootstrap color class for a given project status.
        /// </summary>
        public static string GetColorClass(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "bg-secondary";
            return StatusMap.TryGetValue(status, out var val) ? val.ColorClass : "bg-secondary";
        }
    }
}
