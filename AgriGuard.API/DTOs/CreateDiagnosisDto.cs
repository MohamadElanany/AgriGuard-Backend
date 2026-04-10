using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.DTOs
{
    public class CreateDiagnosisDto
    {
        public int? UserPlantId { get; set; }

        public string PlantName { get; set; } = string.Empty;

        public IFormFile Image { get; set; }
    }
}