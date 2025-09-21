using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Project Manager,Tester")]
    public class ProjectManagerController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public ProjectManagerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // helper: read quotes.json
        private List<Quote> ReadQuotes()
        {
            var path = Path.Combine(_env.WebRootPath, "mock", "quotes.json");
            if (!System.IO.File.Exists(path)) return new List<Quote>();

            var json = System.IO.File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<Quote>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<Quote>();

            // recompute totals like in QuotesController
            foreach (var q in list)
            {
                RecalcTotals(q);
            }

            return list;
        }

        private void RecalcTotals(Quote q)
        {
            q.Subtotal = Math.Round(q.Items.Sum(i => i.LineTotal), 2);
            var markup = Math.Round(q.Subtotal * q.MarkupPercent / 100.0, 2);
            var tax = Math.Round((q.Subtotal + markup) * q.TaxPercent / 100.0, 2);
            q.Total = Math.Round(q.Subtotal + markup + tax, 2);
        }

        public IActionResult Dashboard()
        {
            var quotes = ReadQuotes();

            var vm = new DashboardViewModel
            {
                TotalQuotes = quotes.Count,
                RecentAcceptedQuotes = quotes
                    .Where(q => q.Status == "Accepted")
                    .OrderByDescending(q => q.DecidedAt ?? q.CreatedAt)
                    .Take(3)
                    .ToList()
            };

            return View(vm);
        }
    }
}
