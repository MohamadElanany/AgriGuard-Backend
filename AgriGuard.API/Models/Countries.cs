namespace AgriGuard.API.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
    }
}