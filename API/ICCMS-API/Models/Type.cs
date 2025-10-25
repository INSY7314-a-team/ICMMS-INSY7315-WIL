namespace ICCMS_API.Models
{
    public static class Types
    {
        // ----- Audit Log Types -----
        private static readonly string[] _auditLogTypes = new[]
        {
            "Login Success",
            "Login Failure",
            "Document Download",
            "Document Upload",
            "Document Access",
            "Maintenance Request Created",
            "Maintenance Request Updated",
            "Project Created",
            "Project Updated",
            "Contractor Assignment",
            "Blueprint Processing",
            "Quotation Approved",
            "Quotation Rejected",
            "Progress Report Submitted",
            "Completion Report Submitted",
            "Task Started",
            "Task Completion Approved",
            "Task Completion Rejected",
        };

        public static string[] GetAuditLogTypes() => _auditLogTypes;

        public static bool IsValidAuditLogType(string value) =>
            !string.IsNullOrWhiteSpace(value) && _auditLogTypes.Contains(value);

        // ----- User Roles (example; reuse pattern everywhere) -----
        private static readonly string[] _roles = new[]
        {
            "Admin",
            "ProjectManager",
            "Contractor",
            "Client",
            "Employee",
        };

        public static string[] GetRoles() => _roles;

        public static bool IsValidRole(string value) =>
            !string.IsNullOrWhiteSpace(value) && _roles.Contains(value);

        // Add more picklists here as needed...
    }
}
