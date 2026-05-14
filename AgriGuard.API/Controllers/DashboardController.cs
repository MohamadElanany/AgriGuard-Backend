using AgriGuard.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Require authentication for dashboard access
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }


        // Get dashboard summary data for current user
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            // Get current authenticated user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            // Define today's date range
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Count active plant sessions
            var activePlantsCount = await _context.UserPlants
                .CountAsync(up => up.UserId == userId && up.Status == "InProgress");

            // Count today's pending tasks
            var todayTasksCount = await _context.CareTasks
                .Include(t => t.UserPlant)
                .CountAsync(t =>
                    t.UserPlant.UserId == userId &&
                    t.DueDate >= today &&
                    t.DueDate < tomorrow &&
                    !t.IsCompleted);

            // Get latest diagnosis result
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

            // Get latest approved community posts
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
            
            // Count overdue unfinished tasks
            var overdueTasksCount = await _context.CareTasks
                .Include(t => t.UserPlant)
                .CountAsync(t =>
                    t.UserPlant.UserId == userId &&
                    !t.IsCompleted &&
                    t.DueDate.Date < today);

            // Return dashboard summary data
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