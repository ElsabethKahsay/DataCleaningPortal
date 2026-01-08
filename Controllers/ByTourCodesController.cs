using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("/ByTourCodes")]
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

            // Business Logic: Prevent Duplicates
            bool exists = await _context.ByTourCode.AnyAsync(x =>
                x.TourCode == byTourCode.TourCode &&
                x.Date == byTourCode.Date &&
                x.CORP_TYPE == byTourCode.CORP_TYPE);

            if (exists) return Conflict("Date already exists for this Tour Code.");

            // Process logic (Calculating percents, etc.)
            byTourCode.CreatedAt = DateTime.Now;
            byTourCode.CreatedBy = User.Identity?.Name ?? "System";
            byTourCode.Status = Status.Active;

            // Calculate ATPercent
            if (byTourCode.Target != 0)
                byTourCode.ATPercent = Math.Round((byTourCode.MonthylyAmount - byTourCode.Target) / byTourCode.Target * 100, 2);

            _context.ByTourCode.Add(byTourCode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetails), new { id = byTourCode.Id }, byTourCode);
        }

        // 4. PUT: api/ByTourCodes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(long id, ByTourCode byTourCode)
        {
            if (id != byTourCode.Id) return BadRequest();

            _context.Entry(byTourCode).State = EntityState.Modified;

            try
            {
                byTourCode.UpdatedAt = DateTime.Now;
                byTourCode.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ByTourCodeExists(id)) return NotFound();
                throw;
            }

            return NoContent(); // Standard 204 response for successful updates
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