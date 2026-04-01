namespace AgriGuard.API.DTOs
{
    public class DiagnosisResultDto
    {
        public int Id { get; set; }

        public int UserPlantId { get; set; }

        public string DiseaseName { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string RecommendedAction { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public DateTime DiagnosedAt { get; set; }
    }
}