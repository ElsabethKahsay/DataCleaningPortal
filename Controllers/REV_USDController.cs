using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Http;

namespace ADDPerformance.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RevUsdController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;

        public RevUsdController(UserManager<IdentityUser> userManager, DBContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private static readonly Dictionary<string, (int Number, string FullName)> MonthAbbreviations
            = new()
            {
                {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
                {"APR", (4, "April")},   {"MAY", (5, "May")},     {"JUN", (6, "June")},
                {"JUL", (7, "July")},    {"AUG", (8, "August")},   {"SEP", (9, "September")},
                {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
            };

        // 1. GET: api/RevUsd (Get All)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<REV_USD>>> GetRevUsdData()
        {
            return await _context.REV_USD
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        // 2. GET: api/RevUsd/5 (Details)
        [HttpGet("{id}")]
        public async Task<ActionResult<REV_USD>> GetRevUsdDetails(int id)
        {
            var record = await _context.REV_USD.FindAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        // 3. POST: api/RevUsd (Create)
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<REV_USD>> CreateRevUsd(REV_USD record)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Calculate derived fields
            record.CreatedAt = DateTime.UtcNow;
            record.CreatedBy = currentUser?.UserName ?? "SYSTEM_API";
            record.Status = Status.Active;

            // Auto-calculate AT and ALY if values are provided
            if (record.Target_USD != 0)
                record.AT = Math.Round((record.CY_USD - record.Target_USD) / record.Target_USD * 100, 2);

            record.ALY = Math.Round(record.CY_USD - record.LY_USD, 2);

            _context.REV_USD.Add(record);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRevUsdDetails), new { id = record.Id }, record);
        }

        // 4. PUT: api/RevUsd/5 (Edit)
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditRevUsd(int id, REV_USD record)
        {
            if (id != record.Id)
                return BadRequest("ID mismatch");

            var currentUser = await _userManager.GetUserAsync(User);
            var existing = await _context.REV_USD.FindAsync(id);

            if (existing == null)
                return NotFound();

            // Update allowed fields
            existing.CY_USD = record.CY_USD;
            existing.LY_USD = record.LY_USD;
            existing.Target_USD = record.Target_USD;
            existing.Date = record.Date;

            // Recalculate logic
            existing.AT = existing.Target_USD == 0 ? 0 : Math.Round((existing.CY_USD - existing.Target_USD) / existing.Target_USD * 100, 2);
            existing.ALY = Math.Round(existing.CY_USD - existing.LY_USD, 2);

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = currentUser?.Email ?? "SYSTEM_API";

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RecordExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/RevUsd/5 (Delete)
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteRevUsd(int id)
        {
            var record = await _context.REV_USD.FindAsync(id);
            if (record == null)
                return NotFound();

            _context.REV_USD.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // --- Existing CSV Upload ---
        [HttpPost("upload")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UploadRevUsdCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            IdentityUser? currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var mainList = new List<REV_USD>();
                var updatedList = new List<REV_USD>();

                using var reader = new StreamReader(file.OpenReadStream());
                await reader.ReadLineAsync(); // Skip header

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var values = line.Split(',', StringSplitOptions.TrimEntries);
                    if (values.Length < 4) continue;

                    // Date Parsing Logic
                    string rawDate = values[0].Trim().ToUpper();
                    string[] dateParts = rawDate.Split('-');
                    string monthAbbr = dateParts.Length > 1 ? dateParts[^2] : "";
                    string yearStr = dateParts.Length > 1 ? dateParts[^1] : DateTime.Now.Year.ToString();

                    if (!int.TryParse(yearStr, out int year) || !MonthAbbreviations.TryGetValue(monthAbbr, out var monthInfo))
                        continue;

                    DateTime targetDate = new DateTime(year, monthInfo.Number, 1);

                    if (!double.TryParse(values[1], out double cy) ||
                        !double.TryParse(values[2], out double ly) ||
                        !double.TryParse(values[3], out double target))
                        continue;

                    var existing = await _context.REV_USD.FirstOrDefaultAsync(x => x.Date == targetDate);

                    if (existing != null)
                    {
                        existing.CY_USD = cy;
                        existing.LY_USD = ly;
                        existing.Target_USD = target;
                        existing.AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2);
                        existing.ALY = Math.Round(cy - ly, 2);
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.UpdatedBy = currentUser?.Email ?? "SYSTEM_API";
                        updatedList.Add(existing);
                    }
                    else
                    {
                        mainList.Add(new REV_USD
                        {
                            Date = targetDate,
                            CY_USD = cy,
                            LY_USD = ly,
                            Target_USD = target,
                            Month = monthAbbr,
                            Year = year,
                            MonthNum = monthInfo.Number,
                            MonthName = monthInfo.FullName,
                            AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2),
                            ALY = Math.Round(cy - ly, 2),
                            Status = Status.Active,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUser?.UserName ?? "SYSTEM_API"
                        });
                    }
                }

                if (updatedList.Any()) _context.REV_USD.UpdateRange(updatedList);
                if (mainList.Any()) await _context.REV_USD.AddRangeAsync(mainList);

                await _context.SaveChangesAsync();
                return Ok(new ImportResult { Added = mainList.Count, Updated = updatedList.Count, TotalProcessed = mainList.Count + updatedList.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Processing error", message = ex.Message });
            }
        }

        private bool RecordExists(int id) => _context.REV_USD.Any(e => e.Id == id);

        public class ImportResult
        {
            public string Message { get; set; } = "success";
            public int Added { get; set; }
            public int Updated { get; set; }
            public int TotalProcessed { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }
    }
}