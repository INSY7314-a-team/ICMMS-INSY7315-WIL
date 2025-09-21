using System;
using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class DashboardViewModel
    {
        public int TotalQuotes { get; set; }
        public List<Quote> RecentAcceptedQuotes { get; set; } = new();
    }
}
