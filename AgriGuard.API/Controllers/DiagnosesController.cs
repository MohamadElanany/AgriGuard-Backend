using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using AgriGuard.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriGuard.API.Helpers;
using System.Security.Claims;

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
        private readonly TreatmentService _treatmentService;

        public DiagnosesController(
            AppDbContext context,
            AiDiagnosisService aiDiagnosisService,
            TreatmentService treatmentService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _aiDiagnosisService = aiDiagnosisService;
            _environment = environment;
            _treatmentService = treatmentService;

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

            if (!FileHelper.IsValidImage(dto.Image, out var error))
            {
                return BadRequest(new { message = error });
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

            if (prediction == null || !prediction.Success)
            {
                return BadRequest(new { message = "AI prediction failed" });
            }

            //  Uncertain case
            if (prediction.Status == "Uncertain")
            {
                return BadRequest(new
                {
                    status = "Uncertain",
                    message = prediction.Message,
                    confidence = prediction.Confidence
                });
            }

            //  Mismatch case
            if (prediction.Status == "Mismatch")
            {
                return BadRequest(new
                {
                    status = "Mismatch",
                    message = prediction.Message,
                    confidence = prediction.Confidence
                });
            }

            //  Only allow valid final cases
            if (prediction.Status != "Diseased" && prediction.Status != "Healthy")
            {
                return BadRequest(new { message = "Unknown AI status" });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "diagnoses");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = FileHelper.GenerateSafeFileName(dto.Image.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(fileStream);
            }

            var imageUrl = $"/images/diagnoses/{uniqueFileName}";

            var diagnosis = new Diagnosis
            {
                UserId = userId,
                UserPlantId = dto.UserPlantId,
                ImageUrl = imageUrl,
                DiseaseName = prediction.Disease,
                Confidence = prediction.Confidence,
                RecommendedAction = prediction.Status == "Healthy"
                    ? "The plant appears healthy. Keep monitoring it and continue regular care."
                    : "unavailable",
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

        [HttpPost("treatment")]
        public async Task<IActionResult> GetTreatment([FromBody] GetTreatmentRequestDto dto)
        {
            try
            {
                if (dto.DiagnosisId <= 0)
                {
                    return BadRequest(new { message = "Diagnosis ID is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.DiseaseName))
                {
                    return BadRequest(new { message = "Disease name is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Country) || string.IsNullOrWhiteSpace(dto.Governorate))
                {
                    return BadRequest(new { message = "Country and governorate are required" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                int userId = int.Parse(userIdClaim);

                var currentDiagnosis = await _context.Diagnoses
                    .FirstOrDefaultAsync(d => d.Id == dto.DiagnosisId && d.UserId == userId);

                if (currentDiagnosis == null)
                {
                    return NotFound(new { message = "Diagnosis not found" });
                }

                if (!string.IsNullOrWhiteSpace(currentDiagnosis.TreatmentPlan))
                {
                    return Ok(new
                    {
                        treatmentPlan = currentDiagnosis.TreatmentPlan,
                        preventionTips = string.IsNullOrWhiteSpace(currentDiagnosis.PreventionTipsJson)
                            ? new List<string>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(currentDiagnosis.PreventionTipsJson),
                        recommendedProducts = string.IsNullOrWhiteSpace(currentDiagnosis.RecommendedProductsJson)
                            ? new List<string>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(currentDiagnosis.RecommendedProductsJson),
                        notes = currentDiagnosis.TreatmentNotes ?? ""
                    });
                }

                var cachedDiagnosis = await _context.Diagnoses
                    .Where(d =>
                        d.Id != currentDiagnosis.Id &&
                        d.DiseaseName == dto.DiseaseName &&
                        d.CropName == dto.CropName &&
                        d.Country == dto.Country &&
                        d.Governorate == dto.Governorate &&
                        d.TreatmentPlan != null)
                    .OrderByDescending(d => d.TreatmentGeneratedAt)
                    .FirstOrDefaultAsync();

                if (cachedDiagnosis != null)
                {
                    currentDiagnosis.TreatmentPlan = cachedDiagnosis.TreatmentPlan;
                    currentDiagnosis.PreventionTipsJson = cachedDiagnosis.PreventionTipsJson;
                    currentDiagnosis.RecommendedProductsJson = cachedDiagnosis.RecommendedProductsJson;
                    currentDiagnosis.TreatmentNotes = cachedDiagnosis.TreatmentNotes;
                    currentDiagnosis.TreatmentGeneratedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        treatmentPlan = currentDiagnosis.TreatmentPlan,
                        preventionTips = string.IsNullOrWhiteSpace(currentDiagnosis.PreventionTipsJson)
                            ? new List<string>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(currentDiagnosis.PreventionTipsJson),
                        recommendedProducts = string.IsNullOrWhiteSpace(currentDiagnosis.RecommendedProductsJson)
                            ? new List<string>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(currentDiagnosis.RecommendedProductsJson),
                        notes = currentDiagnosis.TreatmentNotes ?? ""
                    });
                }

                var result = await _treatmentService.GetTreatmentPlanAsync(dto);

                if (result == null)
                {
                    return BadRequest(new { message = "Failed to generate treatment plan" });
                }

                currentDiagnosis.TreatmentPlan = result.TreatmentPlan;
                currentDiagnosis.PreventionTipsJson = System.Text.Json.JsonSerializer.Serialize(result.PreventionTips);
                currentDiagnosis.RecommendedProductsJson = System.Text.Json.JsonSerializer.Serialize(result.RecommendedProducts);
                currentDiagnosis.TreatmentNotes = result.Notes;
                currentDiagnosis.TreatmentGeneratedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyDiagnoses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var diagnosesFromDb = await _context.Diagnoses
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DiagnosedAt)
                .Select(d => new
                {
                    d.Id,
                    d.UserPlantId,
                    d.CropName,
                    d.ImageUrl,
                    d.DiseaseName,
                    d.Confidence,
                    d.RecommendedAction,
                    d.TreatmentPlan,
                    d.PreventionTipsJson,
                    d.RecommendedProductsJson,
                    d.TreatmentNotes,
                    d.Country,
                    d.Governorate,
                    d.DiagnosedAt
                })
                .ToListAsync();

            var diagnoses = diagnosesFromDb.Select(d => new
            {
                d.Id,
                d.UserPlantId,
                d.CropName,
                d.ImageUrl,
                d.DiseaseName,
                d.Confidence,
                d.RecommendedAction,
                d.TreatmentPlan,
                PreventionTips = string.IsNullOrWhiteSpace(d.PreventionTipsJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(d.PreventionTipsJson) ?? new List<string>(),
                RecommendedProducts = string.IsNullOrWhiteSpace(d.RecommendedProductsJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(d.RecommendedProductsJson) ?? new List<string>(),
                d.TreatmentNotes,
                d.Country,
                d.Governorate,
                d.DiagnosedAt
            }).ToList();

            return Ok(diagnoses);
        }

        [HttpGet("plant/{userPlantId}")]
        public async Task<IActionResult> GetDiagnosesByPlant(int userPlantId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            int userId = int.Parse(userIdClaim);

            var plantExists = await _context.UserPlants
                .AnyAsync(up => up.Id == userPlantId && up.UserId == userId);

            if (!plantExists)
            {
                return NotFound(new { message = "Plant session not found" });
            }

            var diagnosesFromDb = await _context.Diagnoses
                .Where(d => d.UserPlantId == userPlantId && d.UserId == userId)
                .OrderByDescending(d => d.DiagnosedAt)
                .Select(d => new
                {
                    d.Id,
                    d.UserPlantId,
                    d.CropName,
                    d.ImageUrl,
                    d.DiseaseName,
                    d.Confidence,
                    d.RecommendedAction,
                    d.TreatmentPlan,
                    d.PreventionTipsJson,
                    d.RecommendedProductsJson,
                    d.TreatmentNotes,
                    d.Country,
                    d.Governorate,
                    d.DiagnosedAt
                })
                .ToListAsync();

            var diagnoses = diagnosesFromDb.Select(d => new
            {
                d.Id,
                d.UserPlantId,
                d.CropName,
                d.ImageUrl,
                d.DiseaseName,
                d.Confidence,
                d.RecommendedAction,
                d.TreatmentPlan,
                PreventionTips = string.IsNullOrWhiteSpace(d.PreventionTipsJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(d.PreventionTipsJson) ?? new List<string>(),
                RecommendedProducts = string.IsNullOrWhiteSpace(d.RecommendedProductsJson)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(d.RecommendedProductsJson) ?? new List<string>(),
                d.TreatmentNotes,
                d.Country,
                d.Governorate,
                d.DiagnosedAt
            }).ToList();

            return Ok(diagnoses);
        }

    }
}