namespace AgriGuard.API.Models
{
    public class AiPredictionResponse
    {
        public bool Success { get; set; }

        public string Disease { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string Plant_Requested { get; set; } = string.Empty;
    }
}