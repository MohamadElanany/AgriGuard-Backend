using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Helpers;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Country,
                    u.Governorate,
                    u.Role,
                    u.IsBlocked,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email already exists"
                });
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Country = dto.Country,
                Governorate = dto.Governorate,
                Role = "User",
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "User created successfully",
                userId = user.Id
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            if (user.IsBlocked)
            {
                return Unauthorized(new
                {
                    message = "Your account is blocked"
                });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                user.Id,
                user.FullName,
                user.Email,
                user.Role
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var user = await _context.Users.FindAsync(userId);

            return Ok(new
            {
                message = "You are authenticated",
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Country,
                    user.Governorate,
                    user.ProfileImageUrl,
                    user.Bio
                }
            });
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            user.FullName = dto.FullName;
            user.Country = dto.Country;
            user.Governorate = dto.Governorate;
            user.Bio = dto.Bio;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize]
        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "No image uploaded" });

            if (!FileHelper.IsValidImage(image, out var error))
            {
                return BadRequest(new { message = error });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User ID not found in token" });

            var userId = int.Parse(userIdClaim);

            var uploadsFolder = Path.Combine("wwwroot", "images", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = FileHelper.GenerateSafeFileName(image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.ProfileImageUrl = $"/images/profiles/{fileName}";

            await _context.SaveChangesAsync();

            return Ok(new { imageUrl = user.ProfileImageUrl });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Country,
                    u.Governorate,
                    u.ProfileImageUrl,
                    u.Bio,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/block")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.Role == "Admin")
            {
                return BadRequest(new { message = "Admin user cannot be blocked" });
            }

            if (user.IsBlocked)
            {
                return BadRequest(new { message = "User is already blocked" });
            }

            user.IsBlocked = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User blocked successfully"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/unblock")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!user.IsBlocked)
            {
                return BadRequest(new { message = "User is not blocked" });
            }

            user.IsBlocked = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User unblocked successfully"
            });
        }
    }
}