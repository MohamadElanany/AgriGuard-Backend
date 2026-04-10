namespace AgriGuard.API.DTOs
{
    public class CreateCareTaskTemplateDto
    {
        public int CropId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DaysAfterPlanting { get; set; }

        public string TaskCategory { get; set; } = string.Empty;
    }
}