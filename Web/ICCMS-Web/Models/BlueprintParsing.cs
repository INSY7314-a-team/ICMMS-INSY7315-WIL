using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace ICCMS_Web.Models
{
    public class BlueprintExtract {
        public double TotalAreaSqm { get; set; }
        public Dictionary<string,double> Counts { get; set; } = new(); // e.g., {"Doors":12,"Windows":28}
        public List<QuoteItem> SuggestedItems { get; set; } = new();   // prefilled items
    }

    public interface IBlueprintParser {
        Task<BlueprintExtract> ParseAsync(Stream fileStream, string fileName);
    }

    // Mock parser: reads a JSON extract from mocks instead of real OCR/CV
    public class MockBlueprintParser : IBlueprintParser {
        private readonly IWebHostEnvironment _env;
        public MockBlueprintParser(IWebHostEnvironment env){ _env = env; }
        public async Task<BlueprintExtract> ParseAsync(Stream fileStream, string fileName){
            // ignore input, load a canned result
            var path = Path.Combine(_env.WebRootPath, "mock", "blueprint_extracts", "office_core.json");
            if (!File.Exists(path)) return new BlueprintExtract();
            var json = await File.ReadAllTextAsync(path);
            return System.Text.Json.JsonSerializer.Deserialize<BlueprintExtract>(json,
                new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true}) ?? new BlueprintExtract();
        }
    }
}
