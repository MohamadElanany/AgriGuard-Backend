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

        // Get all available crops
        public async Task<IActionResult> GetAllCrops()
        {
            var crops = await _context.Crops.ToListAsync();
            return Ok(crops);
        }

        // Allow only admins to create crops
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCrop([FromForm] CreateCropDto dto)
        {
            // Store uploaded crop image path
            string imageUrl = string.Empty;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Validate uploaded image
                if (!FileHelper.IsValidImage(dto.Image, out var error))
                {
                    return BadRequest(new { message = error });
                }

                // Define crop image upload folder
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "crops");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique image file name
                var uniqueFileName = FileHelper.GenerateSafeFileName(dto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save image to server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                imageUrl = $"/images/crops/{uniqueFileName}";
            }

            // Create new crop entity
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

        // Allow admins to update crop data
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCrop(int id, [FromForm] UpdateCropDto dto)
        {
            // Find crop by ID
            var crop = await _context.Crops.FindAsync(id);

            if (crop == null)
            {
                return NotFound(new { message = "Crop not found" });
            }

            // Update crop information
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

        // Allow admins to delete crops
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCrop(int id)
        {
            var crop = await _context.Crops.FindAsync(id);

            if (crop == null)
            {
                return NotFound(new { message = "Crop not found" });
            }

            // Prevent deleting crops linked to existing data
            var hasRelatedUserPlants = await _context.UserPlants.AnyAsync(up => up.CropId == id);
            var hasRelatedTemplates = await _context.CareTaskTemplates.AnyAsync(t => t.CropId == id);

            if (hasRelatedUserPlants || hasRelatedTemplates)
            {
                return BadRequest(new
                {
                    message = "Cannot delete this crop because it is linked to plant sessions or task templates"
                });
            }

            // Remove crop from database
            _context.Crops.Remove(crop);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Crop deleted successfully"
            });
        }
    }
}