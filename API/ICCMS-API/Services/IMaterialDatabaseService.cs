using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IMaterialDatabaseService
    {
        Task<List<MaterialItem>> GetAllMaterialsAsync();
        Task<MaterialItem?> GetMaterialByIdAsync(string materialId);
        Task<List<MaterialItem>> GetMaterialsByCategoryAsync(string category);
        Task<MaterialItem?> GetMaterialByNameAsync(string name);
        Task<double> GetUnitPriceAsync(string materialId);
        Task<List<string>> GetCategoriesAsync();
        Task<List<string>> GetUnitsAsync();
    }

    public class MaterialItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
