using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.DTOs
{
    public class CreateDiagnosisDto
    {
        public int UserPlantId { get; set; }

        public IFormFile Image { get; set; }
    }
}