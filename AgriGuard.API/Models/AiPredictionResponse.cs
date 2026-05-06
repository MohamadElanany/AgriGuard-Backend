namespace AgriGuard.API.Models
{
    public class AiPredictionResponse
    {
        public bool Success { get; set; }

        public string Plant { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Disease { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}