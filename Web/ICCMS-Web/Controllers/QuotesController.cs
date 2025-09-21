using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles="Project Manager,Admin,Tester")]
    public class QuotesController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public QuotesController(IWebHostEnvironment env){ _env = env; }

        private string MockPath(string rel) => Path.Combine(_env.WebRootPath, rel.Replace('/','\\'));

        // ---------- LIST ----------
        [AllowAnonymous]
        public IActionResult Index(){
            var quotes = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            foreach (var q in quotes) RecalcTotals(q);
            return View("~/Views/Quotes/Index.cshtml", quotes.OrderByDescending(q=>q.CreatedAt).ToList());
        }

        // ---------- PREVIEW (from wizard) ----------
        // Receives the QuotePreviewVM from the hidden form (wizard).
        // We compute subtotals here so the preview page shows the right numbers.
        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public IActionResult Preview(QuotePreviewVM vm)
        {
            // If Items are coming as a JSON blob (safer for complex lists), hydrate them
            var itemsJson = Request.Form["ItemsJson"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try {
                    vm.Items = JsonSerializer.Deserialize<List<PreviewItem>>(itemsJson,
                        new JsonSerializerOptions{ PropertyNameCaseInsensitive = true }) ?? new();
                } catch { vm.Items = new(); }
            }

            // Calc
            vm.Subtotal = Math.Round(vm.Items.Sum(i => i.Qty * i.UnitPrice), 2);
            vm.MarkupAmount = Math.Round(vm.Subtotal * vm.MarkupPercent / 100.0, 2);
            vm.TaxAmount = Math.Round((vm.Subtotal + vm.MarkupAmount) * vm.TaxPercent / 100.0, 2);
            vm.Total = Math.Round(vm.Subtotal + vm.MarkupAmount + vm.TaxAmount, 2);

            return View("~/Views/Quotes/Preview.cshtml", vm);
        }

        // ---------- CREATE & SEND (from preview) ----------
        // Takes the same VM again, converts to Quote model, saves into mock/quotes.json,
        // and marks it “Waiting Approval” + SentAt now (notification = stub for later).
        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public IActionResult CreateFromPreview(QuotePreviewVM vm)
        {
            // hydrate items if they came as a JSON blob
            var itemsJson = Request.Form["ItemsJson"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try {
                    vm.Items = JsonSerializer.Deserialize<List<PreviewItem>>(itemsJson,
                        new JsonSerializerOptions{ PropertyNameCaseInsensitive = true }) ?? new();
                } catch { vm.Items = new(); }
            }

            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();

            var q = new Quote {
                Id = "Q-" + (1000 + list.Count + 1),
                ProjectId = vm.ProjectId ?? "",
                ClientName = vm.ClientName ?? "",
                Title = vm.Title ?? $"Quote for {vm.ClientName}",
                Status = "Waiting Approval", // per your request
                MarkupPercent = vm.MarkupPercent,
                TaxPercent = vm.TaxPercent,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow, // we “send” it as part of this action (notifications later)
                Items = vm.Items.Select(i => new QuoteItem{
                    Type = i.Type ?? "Material",
                    Name = i.Name ?? "",
                    Qty = i.Qty,
                    Unit = i.Unit ?? "ea",
                    UnitPrice = i.UnitPrice,
                    ContractorId = i.ContractorId,
                    ContractorName = i.ContractorName
                }).ToList()
            };

            RecalcTotals(q);
            list.Add(q);
            WriteMock("mock/quotes.json", list);

            TempData["ok"] = "Quote created & sent (mock). Client notifications are stubbed.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- SEND (optional: if you want separate send step) ----------
        [HttpPost, AllowAnonymous]
        public IActionResult Send(string id){
            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            var q = list.FirstOrDefault(x=>x.Id==id);
            if (q==null) return NotFound();
            q.Status = "Waiting Approval";
            q.SentAt = DateTime.UtcNow;
            WriteMock("mock/quotes.json", list);
            TempData["ok"] = "Quote sent (mock).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACCEPT / REJECT (dev-only buttons for now) ----------
        [AllowAnonymous]
        public IActionResult Accept(string id){
            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            var q = list.FirstOrDefault(x=>x.Id==id);
            if (q==null) return NotFound();
            q.Status = "Accepted"; q.DecidedAt = DateTime.UtcNow;
            WriteMock("mock/quotes.json", list);
            TempData["ok"] = "Quote accepted (mock).";
            return RedirectToAction(nameof(Index));
        }
        [AllowAnonymous]
        public IActionResult Reject(string id){
            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            var q = list.FirstOrDefault(x=>x.Id==id);
            if (q==null) return NotFound();
            q.Status = "Rejected"; q.DecidedAt = DateTime.UtcNow;
            WriteMock("mock/quotes.json", list);
            TempData["ok"] = "Quote rejected (mock).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- helpers ----------
        private T? ReadMock<T>(string rel){
            try{
                var path = MockPath(rel);
                if (!System.IO.File.Exists(path)) return default;
                var json = System.IO.File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
            }catch{ return default; }
        }
        private void WriteMock<T>(string rel, T data){
            var path = MockPath(rel);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions{WriteIndented=true});
            System.IO.File.WriteAllText(path, json);
        }
        private void RecalcTotals(Quote q){
            q.Subtotal = Math.Round(q.Items.Sum(i=>i.LineTotal),2);
            var markup = Math.Round(q.Subtotal * q.MarkupPercent/100.0,2);
            var taxed  = Math.Round((q.Subtotal + markup) * q.TaxPercent/100.0,2);
            q.Total = Math.Round(q.Subtotal + markup + taxed,2);
        }


        
    }
}
