using Microsoft.AspNetCore.Http;

namespace AgriGuard.API.DTOs
{
    public class UpdateCropDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }

        public int SunHours { get; set; }

        public string WaterLevel { get; set; } = string.Empty;

        public int GrowthDurationDays { get; set; }
    }
}