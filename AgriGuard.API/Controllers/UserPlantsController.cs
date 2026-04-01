using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserPlantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserPlantsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUserPlants()
        {
            var userPlants = await _context.UserPlants
                .Include(up => up.User)
                .Include(up => up.Crop)
                .Select(up => new
                {
                    up.Id,
                    up.UserId,
                    UserName = up.User.FullName,
                    up.CropId,
                    CropName = up.Crop.Name,
                    up.PlantingDate,
                    up.Status
                })
                .ToListAsync();

            return Ok(userPlants);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserPlant(CreateUserPlantDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            var cropExists = await _context.Crops.AnyAsync(c => c.Id == dto.CropId);
            if (!cropExists)
            {
                return BadRequest(new { message = "Crop not found" });
            }

            var userPlant = new UserPlant
            {
                UserId = dto.UserId,
                CropId = dto.CropId,
                PlantingDate = dto.PlantingDate,
                Status = "Healthy"
            };

            await _context.UserPlants.AddAsync(userPlant);
            await _context.SaveChangesAsync();

            var templates = await _context.CareTaskTemplates
                .Where(t => t.CropId == dto.CropId)
                .ToListAsync();

            var careTasks = templates.Select(t => new CareTask
            {
                UserPlantId = userPlant.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = dto.PlantingDate.AddDays(t.DaysAfterPlanting),
                IsCompleted = false
            }).ToList();

            if (careTasks.Any())
            {
                await _context.CareTasks.AddRangeAsync(careTasks);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "User plant created successfully and care tasks generated"
            });
        }
    }
}