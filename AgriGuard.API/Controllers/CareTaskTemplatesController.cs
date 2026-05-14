using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriGuard.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CareTaskTemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CareTaskTemplatesController(AppDbContext context)
        {
            _context = context;
        }

        // Get all task templates with related crop data
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _context.CareTaskTemplates
                .Include(t => t.Crop)
                .Select(t => new
                {
                    t.Id,
                    t.CropId,
                    CropName = t.Crop.Name,
                    t.Title,
                    t.Description,
                    t.DaysAfterPlanting,
                    t.TaskCategory
                })
                .ToListAsync();

            return Ok(templates);
        }

        // Allow only admins to create templates
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateTemplate(CreateCareTaskTemplateDto dto)
        {
            // Validate crop existence before creating template
            var cropExists = await _context.Crops.AnyAsync(c => c.Id == dto.CropId);
            if (!cropExists)
            {
                return BadRequest(new { message = "Crop not found" });
            }

            // Create new care task template
            var template = new CareTaskTemplate
            {
                CropId = dto.CropId,
                Title = dto.Title,
                Description = dto.Description,
                DaysAfterPlanting = dto.DaysAfterPlanting,
                TaskCategory = dto.TaskCategory
            };

            // Save template to database
            await _context.CareTaskTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Template created successfully"
            });
        }
    }
}