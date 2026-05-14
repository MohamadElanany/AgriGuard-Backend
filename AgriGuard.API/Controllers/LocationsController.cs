using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LocationsController(AppDbContext context)
        {
            _context = context;
        }

        // Get all available countries
        [HttpGet("countries")]
        public async Task<IActionResult> GetCountries()
        {
            var countries = await _context.Countries
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name
                })
                .ToListAsync();

            return Ok(countries);
        }

        // Get governorates for selected country
        [HttpGet("governorates/{countryId}")]
        public async Task<IActionResult> GetGovernorates(int countryId)
        {
            var governorates = await _context.Governorates
                .Where(g => g.CountryId == countryId)
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    g.Id,
                    g.Name
                })
                .ToListAsync();

            return Ok(governorates);
        }

        // Allow admins to add new countries
        [Authorize(Roles = "Admin")]
        [HttpPost("countries")]
        public async Task<IActionResult> AddCountry([FromBody] CreateCountryDto dto)
        {
            // Validate country name
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Country name is required" });
            }

            // Prevent duplicate country names
            var exists = await _context.Countries.AnyAsync(c => c.Name == dto.Name);
            if (exists)
            {
                return BadRequest(new { message = "Country already exists" });
            }

            // Create new country entity
            var country = new Country
            {
                Name = dto.Name.Trim()
            };

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Country added successfully" });
        }

        // Allow admins to add governorates
        [Authorize(Roles = "Admin")]
        [HttpPost("governorates")]
        public async Task<IActionResult> AddGovernorate([FromBody] CreateGovernorateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Governorate name is required" });
            }

            // Ensure selected country exists
            var countryExists = await _context.Countries.AnyAsync(c => c.Id == dto.CountryId);
            if (!countryExists)
            {
                return BadRequest(new { message = "Country not found" });
            }

            // Prevent duplicate governorates within the same country
            var exists = await _context.Governorates.AnyAsync(g =>
                g.Name == dto.Name && g.CountryId == dto.CountryId);

            if (exists)
            {
                return BadRequest(new { message = "Governorate already exists for this country" });
            }

            // Create new governorate entity
            var governorate = new Governorate
            {
                Name = dto.Name.Trim(),
                CountryId = dto.CountryId
            };

            _context.Governorates.Add(governorate);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Governorate added successfully" });
        }
    }
}