namespace AgriGuard.API.DTOs
{
    public class CreateUserPlantDto
    {
        public int UserId { get; set; }

        public int CropId { get; set; }

        public DateTime PlantingDate { get; set; }
    }
}