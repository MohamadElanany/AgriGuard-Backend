using AgriGuard.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalDiagnoses = await _context.Diagnoses.CountAsync();

            var topDiseases = await _context.Diagnoses
                .GroupBy(d => d.DiseaseName)
                .Select(g => new
                {
                    name = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            var topCrops = await _context.Diagnoses
                .GroupBy(d => d.CropName)
                .Select(g => new
                {
                    name = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            var topLocations = await _context.Diagnoses
                .GroupBy(d => d.Governorate)
                .Select(g => new
                {
                    name = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                totalDiagnoses,
                topDiseases,
                topCrops,
                topLocations
            });
        }
    }
}