using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    // Data passed from controller → dashboard view
    public class DashboardViewModel
    {
        public int TotalQuotes { get; set; }
        public List<QuotationDto> RecentAcceptedQuotes { get; set; } = new();
    }
}
