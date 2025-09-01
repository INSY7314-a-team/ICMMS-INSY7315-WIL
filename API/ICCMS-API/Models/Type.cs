namespace ICCMS_API.Models
{
    public static class Types
    {
        // ----- Audit Log Types -----
        private static readonly string[] _auditLogTypes = new[]
        {
            "Login Attempt",
            "Document Download",
            "Document Upload",
            "Maintenance Update",
            "Project Update",
            "Quotation"
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
            "Employee"
        };

        public static string[] GetRoles() => _roles;
        public static bool IsValidRole(string value) =>
            !string.IsNullOrWhiteSpace(value) && _roles.Contains(value);


        // Add more picklists here as needed...
    }
}
