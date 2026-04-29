using AgriGuard.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }



        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var activePlantsCount = await _context.UserPlants
                .CountAsync(up => up.UserId == userId && up.Status == "InProgress");

            var todayTasksCount = await _context.CareTasks
                .Include(t => t.UserPlant)
                .CountAsync(t =>
                    t.UserPlant.UserId == userId &&
                    t.DueDate >= today &&
                    t.DueDate < tomorrow &&
                    !t.IsCompleted);

            var latestDiagnosis = await _context.Diagnoses
                 .Where(d => d.UserId == userId)
                 .OrderByDescending(d => d.DiagnosedAt)
                 .Select(d => new
                 {
                     d.Id,
                     d.DiseaseName,
                     d.Confidence,
                     d.ImageUrl,
                     d.CropName,
                     d.DiagnosedAt
                 })
                 .FirstOrDefaultAsync();

            var recentPosts = await _context.Posts
                .Include(p => p.User)
                .Where(p => p.Status == "Approved")
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    UserName = p.User.FullName,
                    p.Content,
                    p.ImageUrl,
                    p.CreatedAt,
                    ProfileImageUrl = p.User.ProfileImageUrl,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count
                })
                .ToListAsync();


            var overdueTasksCount = await _context.CareTasks
                .Include(t => t.UserPlant)
                .CountAsync(t =>
                    t.UserPlant.UserId == userId &&
                    !t.IsCompleted &&
                    t.DueDate.Date < today);

            return Ok(new
            {
                activePlantsCount,
                todayTasksCount,
                overdueTasksCount,
                latestDiagnosis,
                recentPosts
            });
        }
    }
}