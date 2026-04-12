namespace AgriGuard.API.DTOs
{
    public class GetTreatmentRequestDto
    {
        public string DiseaseName { get; set; } = string.Empty;
        public string CropName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
    }
}