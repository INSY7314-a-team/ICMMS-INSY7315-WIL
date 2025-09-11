using System.Text.Json;
using System.Linq;
using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class MaterialDatabaseService : IMaterialDatabaseService
    {
        private readonly string _databasePath;
        private List<MaterialItem>? _materials;
        private List<string>? _categories;
        private List<string>? _units;

        public MaterialDatabaseService()
        {
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ConstructionMaterialsDatabase.json");
        }

        private async Task LoadDatabaseAsync()
        {
            if (_materials != null) return;

            try
            {
                var json = await File.ReadAllTextAsync(_databasePath);
                
                // Parse the JSON as an object with materials array
                using var document = JsonDocument.Parse(json);
                var materialsArray = new List<MaterialItem>();
                
                if (document.RootElement.TryGetProperty("materials", out var materialsElement))
                {
                    foreach (var materialElement in materialsElement.EnumerateArray())
                    {
                        var material = new MaterialItem
                        {
                            Id = materialElement.GetProperty("id").GetString() ?? "",
                            Name = materialElement.GetProperty("name").GetString() ?? "",
                            Unit = materialElement.GetProperty("unit").GetString() ?? "",
                            UnitPrice = materialElement.GetProperty("unitPrice").GetDouble(),
                            Category = materialElement.GetProperty("category").GetString() ?? "",
                            Description = materialElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                            LastUpdated = materialElement.TryGetProperty("lastUpdated", out var lastUpdated) && 
                                        DateTime.TryParse(lastUpdated.GetString(), out var date) ? date : DateTime.UtcNow
                        };
                        materialsArray.Add(material);
                    }
                }
                
                _materials = materialsArray;
                
                // Extract unique categories and units from materials
                _categories = _materials.Select(m => m.Category).Distinct().ToList();
                _units = _materials.Select(m => m.Unit).Distinct().ToList();
                
                Console.WriteLine($"Loaded {_materials.Count} materials from database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading material database: {ex.Message}");
                // Log error and initialize with empty data
                _materials = new List<MaterialItem>();
                _categories = new List<string>();
                _units = new List<string>();
            }
        }

        public async Task<List<MaterialItem>> GetAllMaterialsAsync()
        {
            await LoadDatabaseAsync();
            return _materials ?? new List<MaterialItem>();
        }

        public async Task<MaterialItem?> GetMaterialByIdAsync(string materialId)
        {
            await LoadDatabaseAsync();
            return _materials?.FirstOrDefault(m => m.Id == materialId);
        }

        public async Task<List<MaterialItem>> GetMaterialsByCategoryAsync(string category)
        {
            await LoadDatabaseAsync();
            return _materials?.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<MaterialItem>();
        }

        public async Task<MaterialItem?> GetMaterialByNameAsync(string name)
        {
            await LoadDatabaseAsync();
            return _materials?.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<double> GetUnitPriceAsync(string materialId)
        {
            var material = await GetMaterialByIdAsync(materialId);
            return material?.UnitPrice ?? 0.0;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            await LoadDatabaseAsync();
            return _categories ?? new List<string>();
        }

        public async Task<List<string>> GetUnitsAsync()
        {
            await LoadDatabaseAsync();
            return _units ?? new List<string>();
        }
    }

    public class MaterialDatabase
    {
        public List<MaterialItem> Materials { get; set; } = new List<MaterialItem>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Units { get; set; } = new List<string>();
    }
}
