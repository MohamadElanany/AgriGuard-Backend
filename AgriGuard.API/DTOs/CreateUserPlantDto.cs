namespace AgriGuard.API.DTOs
{
    public class CreateUserPlantDto
    {
        public int CropId { get; set; }

        public string CustomTitle { get; set; } = string.Empty;

        public DateTime PlantingDate { get; set; }
    }
}