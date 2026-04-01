namespace AgriGuard.API.Models
{
    public class CareTask
    {
        public int Id { get; set; }

        public int UserPlantId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public UserPlant UserPlant { get; set; }
    }
}