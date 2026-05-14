using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Helpers;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PostsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Get all community posts
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            // Get current user role
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Load posts with related user, comments, and likes
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .AsQueryable();

            // Non-admin users can only see approved posts
            if (userRole != "Admin")
            {
                query = query.Where(p => p.Status == "Approved");
            }

            // Get current authenticated user ID
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = 0;

            if (!string.IsNullOrEmpty(currentUserIdClaim))
            {
                currentUserId = int.Parse(currentUserIdClaim);
            }

            // Prepare posts response with likes and comments data
            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    UserName = p.User.FullName,
                    p.Content,
                    p.ImageUrl,
                    p.Status,
                    p.CreatedAt,
                    ProfileImageUrl = p.User.ProfileImageUrl,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
                    Comments = p.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.UserId,
                            UserName = c.User.FullName,
                            c.Content,
                            c.CreatedAt,
                            ProfileImageUrl = c.User.ProfileImageUrl
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }

        // Create new community post
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {

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

            // Require post content or image
            if (string.IsNullOrWhiteSpace(dto.Content) && dto.Image == null)
            {
                return BadRequest(new { message = "Post content or image is required" });
            }

            // Store uploaded post image path
            string imageUrl = string.Empty;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Validate uploaded image
                if (!FileHelper.IsValidImage(dto.Image, out var error))
                {
                    return BadRequest(new { message = error });
                }

                // Define post image upload folder
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "posts");

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

                imageUrl = $"/images/posts/{uniqueFileName}";
            }

            // Create new post entity
            var post = new Post
            {
                UserId = userId,
                Content = dto.Content ?? string.Empty,
                ImageUrl = imageUrl,
                // Admin posts are approved automatically
                Status = User.IsInRole("Admin") ? "Approved" : "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Post created successfully",
                postId = post.Id
            });
        }

        // Allow admins to approve posts
        [Authorize(Roles = "Admin")]
        [HttpPut("{postId}/approve")]
        public async Task<IActionResult> ApprovePost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            // Update post status to approved
            post.Status = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post approved successfully"
            });
        }

        // Allow admins to reject posts
        [Authorize(Roles = "Admin")]
        [HttpPut("{postId}/reject")]
        public async Task<IActionResult> RejectPost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            // Update post status to rejected
            post.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post rejected successfully"
            });
        }

        // Delete post by owner or admin
        [Authorize]
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            // Prevent unauthorized post deletion
            if (userRole != "Admin" && post.UserId != userId)
            {
                return Forbid();
            }

            // Remove post image from server
            if (!string.IsNullOrWhiteSpace(post.ImageUrl))
            {
                var relativePath = post.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            // Remove post from database
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post deleted successfully"
            });
        }

        // Add comment to community post
        [Authorize]
        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> AddComment(int postId, CreateCommentDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return BadRequest(new { message = "Post not found" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { message = "Comment content is required" });
            }

            // Prevent commenting on unapproved posts
            if (post.Status != "Approved")
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return BadRequest(new { message = "You cannot comment on an unapproved post" });
                }
            }

            // Create new comment entity
            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Comment added successfully"
            });
        }

        // Add like to post
        [Authorize]
        [HttpPost("{postId}/like")]
        public async Task<IActionResult> AddLike(int postId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return BadRequest(new { message = "Post not found" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            if (post.Status != "Approved")
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return BadRequest(new { message = "You cannot like an unapproved post" });
                }
            }

            // Prevent duplicate likes
            var alreadyLiked = await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);

            if (alreadyLiked)
            {
                return BadRequest(new { message = "You already liked this post" });
            }

            // Create new like entity
            var like = new Like
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Likes.AddAsync(like);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post liked successfully"
            });
        }

        // Remove like from post
        [Authorize]
        [HttpDelete("{postId}/like")]
        public async Task<IActionResult> RemoveLike(int postId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null)
            {
                return NotFound(new { message = "Like not found" });
            }

            // Delete like from database
            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Like removed successfully"
            });
        }
    }
}