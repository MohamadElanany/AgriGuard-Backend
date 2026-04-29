using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Helpers;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CropsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCrops()
        {
            var crops = await _context.Crops.ToListAsync();
            return Ok(crops);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCrop([FromForm] CreateCropDto dto)
        {
            string imageUrl = string.Empty;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!FileHelper.IsValidImage(dto.Image, out var error))
                {
                    return BadRequest(new { message = error });
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "crops");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = FileHelper.GenerateSafeFileName(dto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                imageUrl = $"/images/crops/{uniqueFileName}";
            }

            var crop = new Crop
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imageUrl,
                SunHours = dto.SunHours,
                WaterLevel = dto.WaterLevel,
                GrowthDurationDays = dto.GrowthDurationDays
            };

            await _context.Crops.AddAsync(crop);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Crop created successfully",
                cropId = crop.Id
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCrop(int id, [FromForm] UpdateCropDto dto)
        {
            var crop = await _context.Crops.FindAsync(id);

            if (crop == null)
            {
                return NotFound(new { message = "Crop not found" });
            }

            crop.Name = dto.Name;
            crop.Description = dto.Description;
            crop.SunHours = dto.SunHours;
            crop.WaterLevel = dto.WaterLevel;
            crop.GrowthDurationDays = dto.GrowthDurationDays;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!FileHelper.IsValidImage(dto.Image, out var error))
                {
                    return BadRequest(new { message = error });
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "crops");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = FileHelper.GenerateSafeFileName(dto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                crop.ImageUrl = $"/images/crops/{uniqueFileName}";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Crop updated successfully"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCrop(int id)
        {
            var crop = await _context.Crops.FindAsync(id);

            if (crop == null)
            {
                return NotFound(new { message = "Crop not found" });
            }

            var hasRelatedUserPlants = await _context.UserPlants.AnyAsync(up => up.CropId == id);
            var hasRelatedTemplates = await _context.CareTaskTemplates.AnyAsync(t => t.CropId == id);

            if (hasRelatedUserPlants || hasRelatedTemplates)
            {
                return BadRequest(new
                {
                    message = "Cannot delete this crop because it is linked to plant sessions or task templates"
                });
            }

            _context.Crops.Remove(crop);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Crop deleted successfully"
            });
        }
    }
}