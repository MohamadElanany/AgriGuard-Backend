namespace AgriGuard.API.Models
{
    public class Diagnosis
    {
        public int Id { get; set; }

        public int? UserPlantId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string DiseaseName { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string RecommendedAction { get; set; } = string.Empty;

        public DateTime DiagnosedAt { get; set; } = DateTime.UtcNow;

        public string CropName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;

        public UserPlant UserPlant { get; set; }
    }
}