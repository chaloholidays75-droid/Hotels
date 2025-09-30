using AutoMapper;
using HotelAPI.Data;
using HotelAPI.Models;
using HotelAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public SupplierCategoriesController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/SupplierCategories
        // Added pagination & optional search
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierCategoryDto>>> GetCategories(
            int page = 1, int pageSize = 20, string? search = null)
        {
            var query = _context.SupplierCategories
                .Include(c => c.SubCategories)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var resultDto = _mapper.Map<List<SupplierCategoryDto>>(categories);

            return Ok(new
            {
                totalItems,
                page,
                pageSize,
                items = resultDto
            });
        }

        // GET: api/SupplierCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierCategoryDto>> GetCategory(int id)
        {
            var category = await _context.SupplierCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            return Ok(_mapper.Map<SupplierCategoryDto>(category));
        }

        // POST: api/SupplierCategories
        [HttpPost]
        public async Task<ActionResult<SupplierCategoryDto>> CreateCategory([FromBody] SupplierCategoryDto categoryDto)
        {
            var category = _mapper.Map<SupplierCategory>(categoryDto);
            _context.SupplierCategories.Add(category);

            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<SupplierCategoryDto>(category);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, resultDto);
        }

        // PUT: api/SupplierCategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] SupplierCategoryDto categoryDto)
        {
            if (id != categoryDto.Id)
                return BadRequest(new { message = "Category ID mismatch." });

            var category = await _context.SupplierCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            _mapper.Map(categoryDto, category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            {
                return StatusCode(500, new { message = "Database update error.", details = e.Message });
            }

            return Ok(new { message = "Updated successfully" });
        }

        // DELETE: api/SupplierCategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.SupplierCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "Category not found" });

            _context.SupplierCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }

        // POST: api/SupplierCategories/5/subcategories
        [HttpPost("{categoryId}/subcategories")]
        public async Task<ActionResult<SupplierSubCategoryDto>> AddSubCategory(int categoryId, [FromBody] SupplierSubCategoryDto subCategoryDto)
        {
            var category = await _context.SupplierCategories.FindAsync(categoryId);
            if (category == null)
                return NotFound(new { message = "Parent category not found." });

            var subCategory = _mapper.Map<SupplierSubCategory>(subCategoryDto);
            subCategory.SupplierCategoryId = categoryId;

            _context.SupplierSubCategories.Add(subCategory);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<SupplierSubCategoryDto>(subCategory);
            return CreatedAtAction(nameof(GetCategory), new { id = categoryId }, resultDto);
        }

        // DELETE: api/SupplierCategories/subcategories/5
        [HttpDelete("subcategories/{subCategoryId}")]
        public async Task<IActionResult> DeleteSubCategory(int subCategoryId)
        {
            var subCategory = await _context.SupplierSubCategories.FindAsync(subCategoryId);
            if (subCategory == null)
                return NotFound(new { message = "Subcategory not found." });

            _context.SupplierSubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }

        // PUT: api/SupplierCategories/subcategories/5
        [HttpPut("subcategories/{subCategoryId}")]
        public async Task<IActionResult> UpdateSubCategory(int subCategoryId, [FromBody] SupplierSubCategoryDto subCategoryDto)
        {
            if (subCategoryId != subCategoryDto.Id)
                return BadRequest(new { message = "Subcategory ID mismatch." });

            var subCategory = await _context.SupplierSubCategories.FindAsync(subCategoryId);
            if (subCategory == null)
                return NotFound(new { message = "Subcategory not found." });

            _mapper.Map(subCategoryDto, subCategory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subcategory updated successfully" });
        }

        // GET: api/SupplierCategories/5/subcategories
        [HttpGet("{categoryId}/subcategories")]
        public async Task<ActionResult<IEnumerable<SupplierSubCategoryDto>>> GetSubCategories(int categoryId)
        {
            var subCategories = await _context.SupplierSubCategories
                .Where(s => s.SupplierCategoryId == categoryId)
                .ToListAsync();

            return Ok(_mapper.Map<List<SupplierSubCategoryDto>>(subCategories));
        }

        // Batch delete endpoint (optional for admin)
        // DELETE: api/SupplierCategories/subcategories
        [HttpDelete("subcategories")]
        public async Task<IActionResult> DeleteSubCategories([FromBody] List<int> subCategoryIds)
        {
            var subCategories = await _context.SupplierSubCategories
                .Where(s => subCategoryIds.Contains(s.Id))
                .ToListAsync();

            if (!subCategories.Any())
                return NotFound(new { message = "No subcategories found for deletion." });

            _context.SupplierSubCategories.RemoveRange(subCategories);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subcategories deleted successfully" });
        }
    }
}
