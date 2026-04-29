using AgriGuard.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CareTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CareTasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCareTasks()
        {
            var tasks = await _context.CareTasks
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.User)
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.Crop)
                .Select(t => new
                {
                    t.Id,
                    t.UserPlantId,
                    UserName = t.UserPlant.User.FullName,
                    CropName = t.UserPlant.Crop.Name,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [Authorize]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var task = await _context.CareTasks
                .Include(t => t.UserPlant)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            if (task.UserPlant == null || task.UserPlant.UserId != userId)
            {
                return Forbid();
            }

            if (task.IsCompleted)
            {
                return BadRequest(new { message = "Task is already completed" });
            }

            task.IsCompleted = true;

            var userPlant = task.UserPlant;

            var totalTasks = await _context.CareTasks
                .CountAsync(t => t.UserPlantId == userPlant.Id);

            var completedTasks = await _context.CareTasks
                .CountAsync(t => t.UserPlantId == userPlant.Id && t.IsCompleted);

            if (totalTasks > 0)
            {
                userPlant.ProgressPercentage = (int)Math.Round((completedTasks * 100.0) / totalTasks);
            }
            else
            {
                userPlant.ProgressPercentage = 0;
            }

            userPlant.Status = completedTasks == totalTasks && totalTasks > 0
                ? "Completed"
                : "InProgress";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task marked as completed",
                progressPercentage = userPlant.ProgressPercentage,
                plantStatus = userPlant.Status
            });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCareTasks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var tasks = await _context.CareTasks
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.User)
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.Crop)
                .Where(t => t.UserPlant.UserId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.UserPlantId,
                    UserName = t.UserPlant.User.FullName,
                    CropName = t.UserPlant.Crop.Name,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    Status = t.IsCompleted
                    ? "Completed"
                    : t.DueDate.Date < DateTime.Today
                        ? "Overdue"
                        : "Pending"
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [Authorize]
        [HttpGet("today")]
        public async Task<IActionResult> GetTodayCareTasks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tasks = await _context.CareTasks
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.User)
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.Crop)
                .Where(t => t.UserPlant.UserId == userId
                            && t.DueDate >= today
                            && t.DueDate < tomorrow)
                .Select(t => new
                {
                    t.Id,
                    t.UserPlantId,
                    UserName = t.UserPlant.User.FullName,
                    CropName = t.UserPlant.Crop.Name,
                    CropImageUrl = t.UserPlant.Crop.ImageUrl,
                    CustomTitle = t.UserPlant.CustomTitle,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    Status = t.IsCompleted
                    ? "Completed"
                    : t.DueDate.Date < DateTime.Today
                        ? "Overdue"
                        : "Pending"
                })
                .ToListAsync();

            return Ok(tasks);
        }



        [Authorize]
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueTasks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var today = DateTime.Today;

            var tasks = await _context.CareTasks
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.User)
                .Include(t => t.UserPlant)
                    .ThenInclude(up => up.Crop)
                .Where(t => t.UserPlant.UserId == userId
                            && !t.IsCompleted
                            && t.DueDate.Date < today)
                .Select(t => new
                {
                    t.Id,
                    t.UserPlantId,
                    UserName = t.UserPlant.User.FullName,
                    CropName = t.UserPlant.Crop.Name,
                    CropImageUrl = t.UserPlant.Crop.ImageUrl,
                    CustomTitle = t.UserPlant.CustomTitle,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    Status = "Overdue"
                })
                .ToListAsync();

            return Ok(tasks);
        }

    }
}