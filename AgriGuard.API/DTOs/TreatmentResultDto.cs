namespace AgriGuard.API.DTOs
{
    public class TreatmentResultDto
    {
        public string TreatmentPlan { get; set; } = string.Empty;
        public List<string> PreventionTips { get; set; } = new();
        public List<string> RecommendedProducts { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }
}