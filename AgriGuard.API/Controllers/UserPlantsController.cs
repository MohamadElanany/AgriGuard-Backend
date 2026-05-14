using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        // Get all plant sessions
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

        // Create new plant session for current user
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateUserPlant(CreateUserPlantDto dto)
        {
            // Get current authenticated user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            // Ensure user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            // Ensure selected crop exists
            var cropExists = await _context.Crops.AnyAsync(c => c.Id == dto.CropId);
            if (!cropExists)
            {
                return BadRequest(new { message = "Crop not found" });
            }

            // Create new plant session
            var userPlant = new UserPlant
            {
                UserId = userId,
                CropId = dto.CropId,
                CustomTitle = dto.CustomTitle ?? string.Empty,
                PlantingDate = dto.PlantingDate,
                Status = "InProgress",
                ProgressPercentage = 0
            };

            await _context.UserPlants.AddAsync(userPlant);
            await _context.SaveChangesAsync();

            // Load task templates for selected crop
            var templates = await _context.CareTaskTemplates
                .Where(t => t.CropId == dto.CropId)
                .ToListAsync();

            // Generate care tasks based on planting date
            var careTasks = templates.Select(t => new CareTask
            {
                UserPlantId = userPlant.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = dto.PlantingDate.AddDays(t.DaysAfterPlanting),
                IsCompleted = false
            }).ToList();

            // Save generated care tasks
            if (careTasks.Any())
            {
                await _context.CareTasks.AddRangeAsync(careTasks);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Plant session created successfully",
                userPlantId = userPlant.Id
            });
        }

        // Get current user's plant sessions
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyPlants()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            // Load user plants with related crop data
            var userPlants = await _context.UserPlants
                .Include(up => up.User)
                .Include(up => up.Crop)
                .Where(up => up.UserId == userId)
                .ToListAsync();

            var result = new List<object>();

            // Calculate progress for each plant session
            foreach (var plant in userPlants)
            {
                // Count total and completed tasks
                var totalTasks = await _context.CareTasks
                    .CountAsync(t => t.UserPlantId == plant.Id);

                var completedTasks = await _context.CareTasks
                    .CountAsync(t => t.UserPlantId == plant.Id && t.IsCompleted);

                int calculatedProgress = totalTasks > 0
                    ? (int)Math.Round((completedTasks * 100.0) / totalTasks)
                    : 0;

                // Update plant status automatically
                string calculatedStatus = totalTasks > 0 && completedTasks == totalTasks
                    ? "Completed"
                    : "InProgress";

                if (plant.ProgressPercentage != calculatedProgress || plant.Status != calculatedStatus)
                {
                    plant.ProgressPercentage = calculatedProgress;
                    plant.Status = calculatedStatus;
                }

                // Prepare response data for frontend
                result.Add(new
                {
                    plant.Id,
                    plant.UserId,
                    UserName = plant.User.FullName,
                    plant.CropId,
                    CropName = plant.Crop.Name,
                    CropImageUrl = plant.Crop.ImageUrl,
                    plant.CustomTitle,
                    plant.PlantingDate,
                    Status = calculatedStatus,
                    ProgressPercentage = calculatedProgress,
                    plant.Crop.SunHours,
                    plant.Crop.WaterLevel,
                    plant.Crop.GrowthDurationDays
                });
            }

            // Save updated progress and status
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        // Get detailed plant session data
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserPlantById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            // Ensure plant session belongs to current user
            var userPlant = await _context.UserPlants
                .Include(up => up.Crop)
                .FirstOrDefaultAsync(up => up.Id == id && up.UserId == userId);

            if (userPlant == null)
            {
                return NotFound(new { message = "Plant session not found" });
            }

            // Load care tasks for selected plant
            var tasks = await _context.CareTasks
                .Where(t => t.UserPlantId == userPlant.Id)
                .OrderBy(t => t.DueDate)
                .Select(t => new
                {
                    t.Id,
                    t.UserPlantId,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted
                })
                .ToListAsync();

            int totalTasks = tasks.Count;
            int completedTasks = tasks.Count(t => t.IsCompleted);

            // Calculate progress percentage dynamically
            int calculatedProgress = totalTasks > 0
                ? (int)Math.Round((completedTasks * 100.0) / totalTasks)
                : 0;

            string calculatedStatus = totalTasks > 0 && completedTasks == totalTasks
                ? "Completed"
                : "InProgress";

            if (userPlant.ProgressPercentage != calculatedProgress || userPlant.Status != calculatedStatus)
            {
                userPlant.ProgressPercentage = calculatedProgress;
                userPlant.Status = calculatedStatus;
                await _context.SaveChangesAsync();
            }

            // Return plant details with tasks and progress
            return Ok(new
            {
                userPlant.Id,
                userPlant.CropId,
                CropName = userPlant.Crop.Name,
                CropImageUrl = userPlant.Crop.ImageUrl,
                userPlant.CustomTitle,
                userPlant.PlantingDate,
                Status = calculatedStatus,
                ProgressPercentage = calculatedProgress,
                userPlant.Crop.SunHours,
                userPlant.Crop.WaterLevel,
                userPlant.Crop.GrowthDurationDays,
                Tasks = tasks
            });
        }
    }
}