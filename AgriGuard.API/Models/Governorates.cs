namespace AgriGuard.API.Models
{
    public class Governorate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int CountryId { get; set; }
        public Country? Country { get; set; }
    }
}