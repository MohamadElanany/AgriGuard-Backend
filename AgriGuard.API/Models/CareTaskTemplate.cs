namespace AgriGuard.API.Models
{
    public class CareTaskTemplate
    {
        public int Id { get; set; }

        public int CropId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DaysAfterPlanting { get; set; }

        // Navigation
        public Crop Crop { get; set; }
    }
}