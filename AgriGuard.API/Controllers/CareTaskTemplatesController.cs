using AgriGuard.API.Data;
using AgriGuard.API.DTOs;
using AgriGuard.API.Models;
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
                    t.DaysAfterPlanting
                })
                .ToListAsync();

            return Ok(templates);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate(CreateCareTaskTemplateDto dto)
        {
            var cropExists = await _context.Crops.AnyAsync(c => c.Id == dto.CropId);
            if (!cropExists)
            {
                return BadRequest(new { message = "Crop not found" });
            }

            var template = new CareTaskTemplate
            {
                CropId = dto.CropId,
                Title = dto.Title,
                Description = dto.Description,
                DaysAfterPlanting = dto.DaysAfterPlanting
            };

            await _context.CareTaskTemplates.AddAsync(template);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Template created successfully"
            });
        }
    }
}