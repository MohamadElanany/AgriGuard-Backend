using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using AgriGuard.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiagnosesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AiDiagnosisService _aiDiagnosisService;
        private readonly IWebHostEnvironment _environment;

        public DiagnosesController(
            AppDbContext context,
            AiDiagnosisService aiDiagnosisService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _aiDiagnosisService = aiDiagnosisService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiagnoses()
        {
            var diagnoses = await _context.Diagnoses
                .Include(d => d.UserPlant)
                    .ThenInclude(up => up.User)
                .Include(d => d.UserPlant)
                    .ThenInclude(up => up.Crop)
                .Select(d => new
                {
                    d.Id,
                    d.UserPlantId,
                    UserName = d.UserPlant != null ? d.UserPlant.User.FullName : null,
                    CropName = d.CropName,
                    d.ImageUrl,
                    d.DiseaseName,
                    d.Confidence,
                    d.RecommendedAction,
                    d.Country,
                    d.Governorate,
                    d.DiagnosedAt
                })
                .ToListAsync();

            return Ok(diagnoses);
        }

        [HttpPost("predict")]
        public async Task<IActionResult> Predict([FromForm] CreateDiagnosisDto dto)
        {
            if (dto.Image == null || dto.Image.Length == 0)
            {
                return BadRequest(new { message = "Image is required" });
            }

            if (string.IsNullOrWhiteSpace(dto.PlantName))
            {
                return BadRequest(new { message = "Plant name is required" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            UserPlant? userPlant = null;

            if (dto.UserPlantId.HasValue)
            {
                userPlant = await _context.UserPlants
                    .Include(up => up.Crop)
                    .FirstOrDefaultAsync(up => up.Id == dto.UserPlantId.Value && up.UserId == userId);

                if (userPlant == null)
                {
                    return BadRequest(new { message = "UserPlant not found" });
                }
            }

            var prediction = await _aiDiagnosisService.PredictDiseaseAsync(dto.PlantName, dto.Image);

            if (prediction == null || !prediction.Success || string.IsNullOrWhiteSpace(prediction.Disease))
            {
                return BadRequest(new { message = "AI prediction failed" });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "diagnoses");

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

            var imageUrl = $"/images/diagnoses/{uniqueFileName}";

            var diagnosis = new Diagnosis
            {
                UserPlantId = dto.UserPlantId,
                ImageUrl = imageUrl,
                DiseaseName = prediction.Disease,
                Confidence = prediction.Confidence,
                RecommendedAction = GetRecommendedAction(prediction.Disease),
                DiagnosedAt = DateTime.UtcNow,
                CropName = dto.PlantName,
                Country = user.Country,
                Governorate = user.Governorate
            };

            await _context.Diagnoses.AddAsync(diagnosis);
            await _context.SaveChangesAsync();

            var result = new DiagnosisResultDto
            {
                Id = diagnosis.Id,
                UserPlantId = diagnosis.UserPlantId ?? 0,
                DiseaseName = diagnosis.DiseaseName,
                Confidence = diagnosis.Confidence,
                RecommendedAction = diagnosis.RecommendedAction,
                ImageUrl = diagnosis.ImageUrl,
                DiagnosedAt = diagnosis.DiagnosedAt
            };

            return Ok(result);
        }

        private string GetRecommendedAction(string diseaseName)
        {
            return diseaseName switch
            {
                "Tomato___Early_blight" => "Remove affected leaves and use a suitable fungicide.",
                "Tomato___Late_blight" => "Avoid overhead irrigation and apply anti-blight treatment.",
                "Tomato___healthy" => "The plant looks healthy. Continue regular care.",
                "Potato___Early_blight" => "Remove infected leaves and apply a suitable fungicide. Monitor irrigation carefully.",
                _ => "Consult an agricultural specialist for the best treatment."
            };
        }
    }
}