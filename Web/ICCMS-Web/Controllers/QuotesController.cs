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
        private readonly IBlueprintParser _parser;
        private string MockPath(string rel) => Path.Combine(_env.WebRootPath, rel.Replace('/','\\'));

        public QuotesController(IWebHostEnvironment env, IBlueprintParser parser){
            _env = env; _parser = parser;
        }

        // ---------- LIST ----------
        [AllowAnonymous] // keep anon for tests
        public IActionResult Index(){
            var quotes = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            foreach (var q in quotes) RecalcTotals(q);
            return View("~/Views/Quotes/Index.cshtml", quotes.OrderByDescending(q=>q.CreatedAt).ToList());
        }

        // ---------- CREATE (GET) ----------
        [AllowAnonymous]
        public IActionResult Create(){
            var rates = ReadMock<List<Rate>>("mock/rates.json") ?? new();
            var srs   = ReadMock<List<ServiceRequest>>("mock/service_requests.json") ?? new();
            ViewBag.Rates = rates;
            ViewBag.ServiceRequests = srs;
            return View("~/Views/Quotes/Create.cshtml", new Quote());
        }

        // ---------- CREATE (POST) ----------
        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public IActionResult Create(Quote model)
        {
        // Bind Items from hidden JSON
        var itemsJson = Request.Form["ItemsJson"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(itemsJson))
        {
            try {
                model.Items = System.Text.Json.JsonSerializer.Deserialize<List<QuoteItem>>(
                    itemsJson,
                    new System.Text.Json.JsonSerializerOptions{ PropertyNameCaseInsensitive = true }
                ) ?? new();
            } catch { model.Items = new(); }
        }

        // Defaults (if not set)
        if (model.MarkupPercent <= 0) model.MarkupPercent = 10;
        if (model.TaxPercent    <= 0) model.TaxPercent    = 15;

        // Persist to mocks
        var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
        model.Id = "Q-" + (1000 + list.Count + 1);
        model.CreatedAt = DateTime.UtcNow;
        RecalcTotals(model);
        list.Add(model);
        WriteMock("mock/quotes.json", list);
        TempData["ok"] = "Quote created.";
        return RedirectToAction(nameof(Index));
        }


        // ---------- PARSE BLUEPRINT (Mock) ----------
        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> ParseBlueprint(){
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null) return BadRequest("No file.");
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var extract = await _parser.ParseAsync(ms, file.FileName);
            return Json(extract);
        }

        // ---------- SEND ----------
        [HttpPost, AllowAnonymous]
        public IActionResult Send(string id){
            var list = ReadMock<List<Quote>>("mock/quotes.json") ?? new();
            var q = list.FirstOrDefault(x=>x.Id==id);
            if (q==null) return NotFound();
            // --- API (disabled) ---
            // await _http.PostAsync($"{_apiBase}/quotes/{id}/send", null);

            q.Status = "Sent";
            q.SentAt = DateTime.UtcNow;
            WriteMock("mock/quotes.json", list);
            TempData["ok"] = "Quote sent (mock).";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACCEPT / REJECT (Client flow stub) ----------
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
