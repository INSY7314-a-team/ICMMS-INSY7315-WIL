using Microsoft.AspNetCore.Mvc;
using ICCMS_Web.Services;
using ICCMS_Web.Models;

namespace ICCMS_Web.Controllers
{
    public class QuotesController : Controller
    {
        private readonly IApiClient _apiClient;

        public QuotesController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // /Quotes/Preview/{id}
        public async Task<IActionResult> Preview(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                var quote = await _apiClient.GetAsync<QuotationDto>($"api/quotations/{id}", User);
                if (quote == null) return NotFound();

                return View(quote);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[QuotesController] ERROR fetching quote: " + ex.Message);
                return StatusCode(500, "Failed to load quote");
            }
        }
    }
}
