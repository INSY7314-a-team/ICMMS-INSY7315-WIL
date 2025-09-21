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

        // ---------- PREVIEW ----------
        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public IActionResult Preview(QuotePreviewVM vm)
        {
            var itemsJson = Request.Form["ItemsJson"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try {
                    vm.Items = JsonSerializer.Deserialize<List<PreviewItem>>(itemsJson,
                        new JsonSerializerOptions{ PropertyNameCaseInsensitive = true }) ?? new();
                } catch { vm.Items = new(); }
            }

            vm.Subtotal = Math.Round(vm.Items.Sum(i => i.Qty * i.UnitPrice), 2);
            vm.MarkupAmount = Math.Round(vm.Subtotal * vm.MarkupPercent / 100.0, 2);
            vm.TaxAmount = Math.Round((vm.Subtotal + vm.MarkupAmount) * vm.TaxPercent / 100.0, 2);
            vm.Total = Math.Round(vm.Subtotal + vm.MarkupAmount + vm.TaxAmount, 2);

            return View("~/Views/Quotes/Preview.cshtml", vm);
        }

        // ---------- CREATE & SEND ----------
       [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public IActionResult CreateFromPreview(QuotePreviewVM vm)
        {
            // ---------- 1. Hydrate items from JSON ----------
            var itemsJson = Request.Form["ItemsJson"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try
                {
                    vm.Items = JsonSerializer.Deserialize<List<PreviewItem>>(itemsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch
                {
                    vm.Items = new();
                }
            }

            // ---------- 2. Load all existing quotes ----------
            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();

            // ---------- 3. Handle reopen ----------
            if (!string.IsNullOrWhiteSpace(vm.OriginalQuoteId))
            {
                var old = list.FirstOrDefault(x => x.Id == vm.OriginalQuoteId);
                if (old != null)
                {
                    old.Status = "Rejected & Reopened";
                    old.DecidedAt = DateTime.UtcNow;
                    old.RejectionReason = old.RejectionReason ?? "(Reopened without reason)";

                    Console.WriteLine($"[CreateFromPreview] Marked old quote {old.Id} as Rejected & Reopened");
                }
                else
                {
                    Console.WriteLine($"[CreateFromPreview] WARNING: Could not find old quote {vm.OriginalQuoteId}");
                }
            }

            // ---------- 4. Generate next sequential ID ----------
            int maxNum = list
                .Select(x => int.TryParse(x.Id?.Replace("Q-", ""), out var n) ? n : 0)
                .DefaultIfEmpty(1000)
                .Max();
            string newId = "Q-" + (maxNum + 1);

            // ---------- 5. Create new quote ----------
            var q = new Quote
            {
                Id = newId,
                ProjectId = vm.ProjectId ?? "",
                ClientName = vm.ClientName ?? "",
                Title = vm.Title ?? $"Quote for {vm.ClientName}",
                Status = "Waiting Approval",
                MarkupPercent = vm.MarkupPercent,
                TaxPercent = vm.TaxPercent,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow,
                Items = vm.Items.Select(i => new QuoteItem
                {
                    Type = i.Type ?? "Material",
                    Name = i.Name ?? "",
                    Qty = i.Qty,
                    Unit = i.Unit ?? "ea",
                    UnitPrice = i.UnitPrice,
                    ContractorId = i.ContractorId,
                    ContractorName = i.ContractorName
                }).ToList(),
                OriginalQuoteId = vm.OriginalQuoteId
            };

            Console.WriteLine($"[CreateFromPreview] Created new quote with Id={q.Id}, OriginalQuoteId={q.OriginalQuoteId}");

            // ---------- 6. Save both old + new ----------
            RecalcTotals(q);
            list.Add(q);
            WriteMock("mock/quotes.json", list);

            TempData["ok"] = "Quote created & sent (mock).";
            return RedirectToAction(nameof(Index));
        }


        // ---------- SEND ----------
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

        // ---------- ACCEPT / REJECT ----------
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

        [HttpGet, AllowAnonymous]
        public IActionResult Reject(string id, string? reason)
        {
            try
            {
                Console.WriteLine($"[Reject] Called with id={id}, reason={reason}");

                var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
                Console.WriteLine($"[Reject] Loaded {list.Count} quotes from JSON");

                var q = list.FirstOrDefault(x => x.Id == id);
                if (q == null)
                {
                    Console.WriteLine($"[Reject] Quote with id={id} not found!");
                    return NotFound();
                }

                q.Status = "Rejected";
                q.DecidedAt = DateTime.UtcNow;
                q.RejectionReason = reason ?? "(no reason)";

                Console.WriteLine($"[Reject] Updated quote {q.Id}: Status={q.Status}, Reason={q.RejectionReason}");

                WriteMock("mock/quotes.json", list);
                Console.WriteLine($"[Reject] Saved changes back to quotes.json");

                return Json(new { ok = true, id = q.Id, status = q.Status, reason = q.RejectionReason });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Reject] ERROR: {ex}");
                return Json(new { ok = false, error = ex.ToString() });
            }
        }

        [HttpGet]
        public IActionResult Get(string id)
        {
            var quotes = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            var q = quotes.FirstOrDefault(x => x.Id == id);
            if (q == null) return NotFound();
            return Json(q);
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
