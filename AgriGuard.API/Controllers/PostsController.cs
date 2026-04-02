using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
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
                    LikesCount = p.Likes.Count,
                    Comments = p.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.UserId,
                            UserName = c.User.FullName,
                            c.Content,
                            c.CreatedAt
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(posts);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            string imageUrl = string.Empty;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "posts");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{dto.Image.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                imageUrl = $"/images/posts/{uniqueFileName}";
            }

            var post = new Post
            {
                UserId = dto.UserId,
                Content = dto.Content,
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

        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> AddComment(int postId, CreateCommentDto dto)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
            {
                return BadRequest(new { message = "Post not found" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
            }

            var comment = new Comment
            {
                PostId = postId,
                UserId = dto.UserId,
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


        [HttpPost("{postId}/like")]
        public async Task<IActionResult> AddLike(int postId, int userId)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
            {
                return BadRequest(new { message = "Post not found" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return BadRequest(new { message = "User not found" });
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
    }
}