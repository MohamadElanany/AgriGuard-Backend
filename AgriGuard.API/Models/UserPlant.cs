namespace AgriGuard.API.Models
{
    public class UserPlant
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CropId { get; set; }

        public DateTime PlantingDate { get; set; }

        public string Status { get; set; } = "Healthy";

        // Navigation Properties
        public User User { get; set; }

        public Crop Crop { get; set; }
    }
}