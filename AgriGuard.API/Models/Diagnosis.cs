namespace AgriGuard.API.Models
{
    public class Diagnosis
    {
        public int Id { get; set; }

        public int UserPlantId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string DiseaseName { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string RecommendedAction { get; set; } = string.Empty;

        public DateTime DiagnosedAt { get; set; } = DateTime.UtcNow;

        public UserPlant UserPlant { get; set; }
    }
}