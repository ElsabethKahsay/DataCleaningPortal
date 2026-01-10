using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // This will make the URL: https://localhost:7103/api/ByTourCodes
    // Remove UserManager if it's commented out in Program.cs
    public class ByTourCodesController : ControllerBase
    {
        private readonly DBContext _context;

        public ByTourCodesController(DBContext context) // Removed UserManager
        {
            _context = context;
        }
   

        // 1. GET: api/ByTourCodes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ByTourCode>>> GetByTourCodes()
        {
            if (_context.ByTourCode == null) return NotFound("Data set is null.");

            var list = await _context.ByTourCode
                .Where(c => c.Status == Status.Active)
                .ToListAsync();

            return Ok(list);
        }

        // 2. GET: api/ByTourCodes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ByTourCode>> GetDetails(long id)
        {
            var byTourCode = await _context.ByTourCode.FindAsync(id);

            if (byTourCode == null) return NotFound();

            return Ok(byTourCode);
        }

        // 3. POST: api/ByTourCodes
        [HttpPost]
        public async Task<ActionResult<ByTourCode>> Create(ByTourCode byTourCode)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // FIX 1: Normalize Date to the 1st of the month for consistency
            var normalizedDate = new DateTime(byTourCode.Date.Year, byTourCode.Date.Month, 1);

            // Business Logic: Prevent Duplicates
            bool exists = await _context.ByTourCode.AnyAsync(x =>
                x.TourCode == byTourCode.TourCode &&
                x.Date == normalizedDate &&
                x.CORP_TYPE == byTourCode.CORP_TYPE &&
                x.Status == Status.Active); // Only check against active records

            if (exists) return Conflict("A record already exists for this Tour Code in this month.");

            // Set Audit and Logic
            byTourCode.Date = normalizedDate;
            byTourCode.CreatedAt = DateTime.Now;
            byTourCode.CreatedBy = User.Identity?.Name ?? "API_System";
            byTourCode.Status = Status.Active;

            // FIX 2: Centralized Calculation
            CalculateMetrics(byTourCode);

            _context.ByTourCode.Add(byTourCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetails), new { id = byTourCode.Id }, byTourCode);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(long id, ByTourCode updatedData)
        {
            if (id != updatedData.Id) return BadRequest("ID mismatch");

            var existingRecord = await _context.ByTourCode.FindAsync(id);
            if (existingRecord == null) return NotFound();

            // FIX 3: Update only specific fields to protect CreatedAt/CreatedBy
            existingRecord.TourCode = updatedData.TourCode;
            existingRecord.MonthylyAmount = updatedData.MonthylyAmount;
            existingRecord.Target = updatedData.Target;
            existingRecord.CORP_TYPE = updatedData.CORP_TYPE;

            // Normalize date if it was changed
            existingRecord.Date = new DateTime(updatedData.Date.Year, updatedData.Date.Month, 1);

            // Recalculate metrics on edit
            CalculateMetrics(existingRecord);

            existingRecord.UpdatedAt = DateTime.Now;
            existingRecord.UpdatedBy = User.Identity?.Name ?? "API_System";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ByTourCodeExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // FIX 4: Helper method to ensure calculations are identical in Create and Edit
        private void CalculateMetrics(ByTourCode model)
        {
            if (model.Target != 0)
            {
                model.ATPercent = Math.Round((model.MonthylyAmount - model.Target) / model.Target * 100, 2);
            }
            else
            {
                model.ATPercent = 0;
            }
        }
        // 5. DELETE: api/ByTourCodes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.ByTourCode.FindAsync(id);
            if (record == null) return NotFound();

            // Soft delete as per your original logic
            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.Now;

            _context.ByTourCode.Update(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ByTourCodeExists(long id) => (_context.ByTourCode?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}