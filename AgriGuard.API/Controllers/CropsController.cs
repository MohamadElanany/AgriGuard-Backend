using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CropsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCrops()
        {
            var crops = await _context.Crops.ToListAsync();
            return Ok(crops);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCrop(CreateCropDto dto)
        {
            var crop = new Crop
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl
            };

            await _context.Crops.AddAsync(crop);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Crop created successfully",
                cropId = crop.Id
            });
        }
    }
}