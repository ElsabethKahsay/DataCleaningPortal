using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADDPerformance.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DateMastersController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DateMastersController(DBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Get all active date master records (months/years)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DateMaster>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DateMaster>>> GetAll()
        {
            var activeDates = await _context.DateMaster
                .Where(x => x.Status == Status.Active)
                .OrderByDescending(x => x.Date)  // Most recent first
                .ToListAsync();

            return Ok(activeDates);
        }

        /// <summary>
        /// Get a single date master record by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(DateMaster), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DateMaster>> GetById(long id)
        {
            var dateMaster = await _context.DateMaster.FindAsync(id);

            if (dateMaster == null)
            {
                return NotFound();
            }

            return Ok(dateMaster);
        }

        /// <summary>
        /// Create a new date master entry (month/year)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DateMaster), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DateMaster>> Create([FromBody] DateMasterCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize to first day of month
            var normalizedDate = new DateTime(dto.Year, dto.Month, 1);

            // Check for existing active record for this month/year
            bool alreadyExists = await _context.DateMaster
                .AnyAsync(x => x.Date == normalizedDate && x.Status == Status.Active);

            if (alreadyExists)
            {
                return Conflict(new
                {
                    message = "This month and year already exists in the active date masters."
                });
            }

            var entity = new DateMaster
            {
                Date = normalizedDate,
                Month = normalizedDate.ToString("MMM").ToUpper(),
                MonthName = normalizedDate.ToString("MMMM"),
                MonthNum = normalizedDate.Month,
                Year = normalizedDate.Year,

                // Audit fields
                Status = Status.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = await GetCurrentUsernameAsync() ?? "SYSTEM_API"
            };

            _context.DateMaster.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        /// <summary>
        /// Update an existing date master record
        /// (Note: Usually date masters are immutable, but included for completeness)
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(long id, [FromBody] DateMasterUpdateDto dto)
        {
            var existing = await _context.DateMaster.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Normally date masters shouldn't change date — but if allowed:
            var newDate = new DateTime(dto.Year ?? existing.Year, dto.Month ?? existing.MonthNum, 1);

            // Check conflict if date is changing
            if (newDate != existing.Date)
            {
                bool conflict = await _context.DateMaster
                    .AnyAsync(x => x.Date == newDate && x.Status == Status.Active && x.Id != id);

                if (conflict)
                {
                    return Conflict(new { message = "The new month/year already exists." });
                }
            }

            // Update fields (only what's allowed)
            existing.Date = newDate;
            existing.Month = newDate.ToString("MMM").ToUpper();
            existing.MonthName = newDate.ToString("MMMM");
            existing.MonthNum = newDate.Month;
            existing.Year = newDate.Year;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = await GetCurrentUsernameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Soft-delete a date master record
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.DateMaster.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = await GetCurrentUsernameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper to get current user's username safely
        private async Task<string?> GetCurrentUsernameAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.UserName;
        }
    }

    // DTOs - recommended for API to avoid over-posting and have cleaner contracts

    public class DateMasterCreateDto
    {
        public int Year { get; set; }
        public int Month { get; set; }  // 1-12
    }

    public class DateMasterUpdateDto
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
    }
}