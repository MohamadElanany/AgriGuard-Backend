namespace AgriGuard.API.Models
{
    public class UserPlant
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CropId { get; set; }

        public string CustomTitle { get; set; } = string.Empty;

        public DateTime PlantingDate { get; set; }

        public string Status { get; set; } = "InProgress";

        public int ProgressPercentage { get; set; } = 0;

        public User User { get; set; }

        public Crop Crop { get; set; }
    }
}