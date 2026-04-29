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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                .AsQueryable();

            if (userRole != "Admin")
            {
                query = query.Where(p => p.Status == "Approved");
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = 0;

            if (!string.IsNullOrEmpty(currentUserIdClaim))
            {
                currentUserId = int.Parse(currentUserIdClaim);
            }

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

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            if (string.IsNullOrWhiteSpace(dto.Content) && dto.Image == null)
            {
                return BadRequest(new { message = "Post content or image is required" });
            }

            string imageUrl = string.Empty;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                if (!FileHelper.IsValidImage(dto.Image, out var error))
                {
                    return BadRequest(new { message = error });
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "posts");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // ✨ استخدام الفئة المساعدة لتوليد اسم آمن
                var uniqueFileName = FileHelper.GenerateSafeFileName(dto.Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                imageUrl = $"/images/posts/{uniqueFileName}";
            }

            var post = new Post
            {
                UserId = userId,
                Content = dto.Content ?? string.Empty,
                ImageUrl = imageUrl,
                Status = "Pending",
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

        [Authorize(Roles = "Admin")]
        [HttpPut("{postId}/approve")]
        public async Task<IActionResult> ApprovePost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            post.Status = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post approved successfully"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{postId}/reject")]
        public async Task<IActionResult> RejectPost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);

            if (post == null)
            {
                return NotFound(new { message = "Post not found" });
            }

            post.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post rejected successfully"
            });
        }

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

            if (userRole != "Admin" && post.UserId != userId)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(post.ImageUrl))
            {
                var relativePath = post.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post deleted successfully"
            });
        }

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

            if (post.Status != "Approved")
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return BadRequest(new { message = "You cannot comment on an unapproved post" });
                }
            }

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

            var alreadyLiked = await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);

            if (alreadyLiked)
            {
                return BadRequest(new { message = "You already liked this post" });
            }

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

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Like removed successfully"
            });
        }
    }
}