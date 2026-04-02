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

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var task = await _context.CareTasks.FindAsync(id);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            task.IsCompleted = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task marked as completed"
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
                    t.IsCompleted
                })
                .ToListAsync();

            return Ok(tasks);
        }

    }
}