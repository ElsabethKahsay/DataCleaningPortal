using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADDPerformance.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ByTourCodesController : ControllerBase
    {
        private readonly DBContext _context;

        public ByTourCodesController(DBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Get all active ByTourCode records
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ByTourCode>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ByTourCode>>> GetAll()
        {
            if (_context.ByTourCode == null)
            {
                return NotFound("ByTourCode dataset is not available.");
            }

            var records = await _context.ByTourCode
                .Where(c => c.Status == Status.Active)
                .OrderByDescending(c => c.Date)   // Most recent first is common pattern
                .ToListAsync();

            return Ok(records);
        }

        /// <summary>
        /// Get a single ByTourCode by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ByTourCode), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ByTourCode>> GetById(long id)
        {
            var record = await _context.ByTourCode.FindAsync(id);

            return record == null
                ? NotFound()
                : Ok(record);
        }

        /// <summary>
        /// Create a new ByTourCode record
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ByTourCode), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ByTourCode>> Create([FromBody] ByTourCode createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Normalize date to first day of month
            var normalizedDate = new DateTime(createDto.Date.Year, createDto.Date.Month, 1);

            // Check for duplicate (same tour code + month + corp type + active)
            bool duplicateExists = await _context.ByTourCode
                .AnyAsync(x =>
                    x.TourCode == createDto.TourCode &&
                    x.Date == normalizedDate &&
                    x.CORP_TYPE == createDto.CORP_TYPE &&
                    x.Status == Status.Active);

            if (duplicateExists)
            {
                return Conflict(new
                {
                    message = "A record already exists for this Tour Code, month and CORP_TYPE."
                });
            }

            // Prepare new entity
            var entity = new ByTourCode
            {
                TourCode = createDto.TourCode,
                Date = normalizedDate,
                MonthylyAmount = createDto.MonthylyAmount,
                Target = createDto.Target,
                CORP_TYPE = createDto.CORP_TYPE,

                // Audit fields
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "SYSTEM_API",
                Status = Status.Active
            };

            CalculateMetrics(entity);

            _context.ByTourCode.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        /// <summary>
        /// Update an existing ByTourCode record
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(long id, [FromBody] ByTourCode updateDto)
        {
            if (id != updateDto.Id)
            {
                return BadRequest("ID in URL and body must match.");
            }

            var existing = await _context.ByTourCode.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Only update allowed business fields
            existing.TourCode = updateDto.TourCode;
            existing.MonthylyAmount = updateDto.MonthylyAmount;
            existing.Target = updateDto.Target;
            existing.CORP_TYPE = updateDto.CORP_TYPE;

            // Always normalize date
            existing.Date = new DateTime(updateDto.Date.Year, updateDto.Date.Month, 1);

            CalculateMetrics(existing);

            // Audit
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = User.Identity?.Name ?? "SYSTEM_API";

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ByTourCodeExistsAsync(id))
                    return NotFound();

                throw;
            }
        }

        /// <summary>
        /// Soft-delete a ByTourCode record
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.ByTourCode.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = User.Identity?.Name ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helpers
        private async Task<bool> ByTourCodeExistsAsync(long id)
        {
            return await _context.ByTourCode.AnyAsync(e => e.Id == id);
        }

        private static void CalculateMetrics(ByTourCode model)
        {
            model.ATPercent = model.Target != 0
                ? Math.Round((model.MonthylyAmount - model.Target) / model.Target * 100, 2)
                : 0;
        }
    }
}