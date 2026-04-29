namespace AgriGuard.API.DTOs
{
    public class CreateGovernorateDto
    {
        public string Name { get; set; } = string.Empty;
        public int CountryId { get; set; }
    }
}