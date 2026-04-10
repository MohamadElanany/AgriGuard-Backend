namespace AgriGuard.API.Models
{
    public class Crop
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public int SunHours { get; set; }

        public string WaterLevel { get; set; } = string.Empty;

        public int GrowthDurationDays { get; set; }
    }
}