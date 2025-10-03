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
    public class SuppliersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public SuppliersController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> GetSuppliers()
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.SupplierCategory)
                .Include(s => s.SupplierSubCategory)
                .Include(s => s.Country)
                .Include(s => s.City)

                .ToListAsync();

            // return Ok(_mapper.Map<List<SupplierResponseDto>>(suppliers));
            // return Ok(new { message = "Hi I am that problem jisne aapka jina haram karke rakha hai ! " });
            return Ok(new { message = "Hi I am that problem jo vapis aapka jina haram karke rakha hai ! " });
        }

        // GET: api/Suppliers/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SupplierResponseDto>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.SupplierCategory)
                .Include(s => s.SupplierSubCategory)
                .Include(s => s.Country)
                .Include(s => s.City)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (supplier == null)
                return NotFound(new { message = "Supplier not found" });

            return Ok(_mapper.Map<SupplierResponseDto>(supplier));
        }

        // GET: api/Suppliers/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> SearchSuppliers(
            [FromQuery] string? name,
            [FromQuery] int? categoryId,
            [FromQuery] int? subCategoryId,
            [FromQuery] int? countryId,
            [FromQuery] int? cityId)
            // [FromQuery] bool? isActive = true)
        {
            var query = _context.Suppliers
                .Include(s => s.SupplierCategory)
                .Include(s => s.SupplierSubCategory)
                .Include(s => s.Country)
                .Include(s => s.City)
                .AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(s => s.SupplierName.Contains(name));

            if (categoryId.HasValue)
                query = query.Where(s => s.SupplierCategoryId == categoryId.Value);

            if (subCategoryId.HasValue)
                query = query.Where(s => s.SupplierSubCategoryId == subCategoryId.Value);

            if (countryId.HasValue)
                query = query.Where(s => s.CountryId == countryId.Value);

            if (cityId.HasValue)
                query = query.Where(s => s.CityId == cityId.Value);

            // if (isActive.HasValue)
            //     query = query.Where(s => s.IsActive == isActive.Value);

            var suppliers = await query.ToListAsync();
            return Ok(_mapper.Map<List<SupplierResponseDto>>(suppliers));
        }

        // GET: api/Suppliers/paged
        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> GetSuppliersPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.SupplierCategory)
                .Include(s => s.SupplierSubCategory)
                .Include(s => s.Country)
                .Include(s => s.City)
               
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(_mapper.Map<List<SupplierResponseDto>>(suppliers));
        }

        // POST: api/Suppliers
[HttpPost]
public async Task<ActionResult<SupplierResponseDto>> CreateSupplier([FromBody] SupplierRequestDto dto)
{
    // Validate foreign keys
    if (!await _context.SupplierCategories.AnyAsync(c => c.Id == dto.SupplierCategoryId))
        return BadRequest(new { message = "Invalid SupplierCategoryId" });

    if (!await _context.SupplierSubCategories.AnyAsync(sc => sc.Id == dto.SupplierSubCategoryId))
        return BadRequest(new { message = "Invalid SupplierSubCategoryId" });

    // Validate unique UserName (case-insensitive)
    if (!string.IsNullOrWhiteSpace(dto.UserName))
    {
        bool userNameExists = await _context.Suppliers
            .AnyAsync(s => s.UserName != null && EF.Functions.ILike(s.UserName, dto.UserName));
        if (userNameExists)
            return BadRequest(new { message = "UserName already exists" });
    }

    // Validate unique UserEmailId (case-insensitive)
    if (!string.IsNullOrWhiteSpace(dto.UserEmailId))
    {
        bool userEmailExists = await _context.Suppliers
            .AnyAsync(s => s.UserEmailId != null && EF.Functions.ILike(s.UserEmailId, dto.UserEmailId));
        if (userEmailExists)
            return BadRequest(new { message = "UserEmailId already exists" });
    }

    // Validate unique Supplier Contact Email (case-insensitive)
    if (!string.IsNullOrWhiteSpace(dto.EmailId))
    {
        bool contactEmailExists = await _context.Suppliers
            .AnyAsync(s => s.ContactEmail != null && EF.Functions.ILike(s.ContactEmail, dto.EmailId));
        if (contactEmailExists)
            return BadRequest(new { message = "Supplier Contact Email already exists" });
    }

    // Map DTO to entity
    var supplier = _mapper.Map<Supplier>(dto);
    supplier.CreatedAt = DateTime.UtcNow;
    supplier.UpdatedAt = DateTime.UtcNow;
    supplier.IsActive = true;

    // Add to DB
            _context.Suppliers.Add(supplier);
    await _context.SaveChangesAsync();

    // Reload supplier with related entities for response
    var supplierWithRelations = await _context.Suppliers
        .Include(s => s.Country)
        .Include(s => s.City)
        .Include(s => s.SupplierCategory)
        .Include(s => s.SupplierSubCategory)
        .FirstOrDefaultAsync(s => s.Id == supplier.Id);

    var result = _mapper.Map<SupplierResponseDto>(supplierWithRelations);

    return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new
    {
        message = "Supplier created successfully",
        supplier = result
    });
}




        // PUT: api/Suppliers/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SupplierResponseDto>> UpdateSupplier(int id, [FromBody] SupplierRequestDto dto)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return NotFound(new { message = "Supplier not found." });

            // Map updated fields
            _mapper.Map(dto, supplier);
            supplier.UpdatedAt = DateTime.UtcNow;

            // Validate FK
            if (!await _context.SupplierCategories.AnyAsync(c => c.Id == supplier.SupplierCategoryId))
                return BadRequest(new { message = "Invalid SupplierCategoryId" });

            if (supplier.SupplierSubCategoryId != 0 &&
                !await _context.SupplierSubCategories.AnyAsync(sc => sc.Id == supplier.SupplierSubCategoryId))
                return BadRequest(new { message = "Invalid SupplierSubCategoryId" });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Error updating supplier" });
            }

            // Reload supplier with navigation properties
            var supplierWithRelations = await _context.Suppliers
                .Include(s => s.Country)
                .Include(s => s.City)
                .Include(s => s.SupplierCategory)
                .Include(s => s.SupplierSubCategory)
                .FirstOrDefaultAsync(s => s.Id == id);

            var result = _mapper.Map<SupplierResponseDto>(supplierWithRelations);

            return Ok(new
            {
                message = "Supplier updated successfully",
                supplier = result
            });
        }

        // PATCH: api/Suppliers/5/toggle
        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> ToggleSupplierStatus(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return NotFound(new { message = "Supplier not found" });

            supplier.IsActive = !supplier.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Supplier is now {(supplier.IsActive ? "active" : "inactive")}" });
        }

        // DELETE: api/Suppliers/5 (soft delete)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null || !supplier.IsActive)
                return NotFound(new { message = "Supplier not found" });

            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Supplier soft-deleted successfully" });
        }

        // POST: api/Suppliers/batch
        [HttpPost("batch")]
        public async Task<IActionResult> CreateSuppliersBatch([FromBody] List<SupplierRequestDto> dtos)
        {
            var suppliers = new List<Supplier>();
            foreach (var dto in dtos)
            {
                if (!await _context.SupplierCategories.AnyAsync(c => c.Id == dto.SupplierCategoryId) ||
                    !await _context.SupplierSubCategories.AnyAsync(sc => sc.Id == dto.SupplierSubCategoryId))
                    continue; // skip invalid FK

                var supplier = _mapper.Map<Supplier>(dto);
                supplier.CreatedAt = DateTime.UtcNow;
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.IsActive = true;
                suppliers.Add(supplier);
            }

            _context.Suppliers.AddRange(suppliers);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{suppliers.Count} suppliers created successfully" });
        }

        // PATCH: api/Suppliers/batch-toggle
        [HttpPatch("batch-toggle")]
        public async Task<IActionResult> ToggleSuppliersStatus([FromBody] List<int> supplierIds)
        {
            var suppliers = await _context.Suppliers
                .Where(s => supplierIds.Contains(s.Id))
                .ToListAsync();

            if (!suppliers.Any())
                return NotFound(new { message = "No suppliers found" });

            foreach (var s in suppliers)
            {
                s.IsActive = !s.IsActive;
                s.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"{suppliers.Count} suppliers updated successfully" });
        }
    }
}
