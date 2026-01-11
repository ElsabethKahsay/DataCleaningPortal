using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ADDPerformance.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CorporateSalesController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Note: IWebHostEnvironment removed as it's not used in this version

        public CorporateSalesController(DBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        private static readonly Dictionary<string, (int Number, string FullName)> MonthMap = new()
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")},   {"MAY", (5, "May")},     {"JUN", (6, "June")},
            {"JUL", (7, "July")},    {"AUG", (8, "August")},   {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        /// <summary>
        /// Get all active corporate sales records
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CorporateSales>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CorporateSales>>> GetAll()
        {
            var records = await _context.CorporateSales
                .Where(x => x.Status == Status.Active)
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.CorpType)
                .ToListAsync();

            return Ok(records);
        }

        /// <summary>
        /// Get a single corporate sales record by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(CorporateSales), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CorporateSales>> GetById(long id)
        {
            var record = await _context.CorporateSales.FindAsync(id);
            return record == null ? NotFound() : Ok(record);
        }

        /// <summary>
        /// Upload and process Corporate Sales CSV file (upsert behavior)
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Policy = "AdminOnly")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only CSV files are allowed" });

            string? currentUserName = await GetCurrentUserNameAsync();

            var added = new List<CorporateSales>();
            var updatedCount = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            bool isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var values = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 5) continue;

                // Expected format: JAN-2024,CorpType,CY,LY,Target
                string[] dateParts = values[0].Split('-');
                if (dateParts.Length != 2) continue;

                string monthAbbr = dateParts[0].ToUpper().Trim();
                if (!MonthMap.TryGetValue(monthAbbr, out var monthInfo)) continue;

                if (!int.TryParse(dateParts[1], out int year)) continue;

                var targetDate = new DateTime(year, monthInfo.Number, 1);
                string corpType = values[1].Trim();

                if (!double.TryParse(values[2], out double cy) ||
                    !double.TryParse(values[3], out double ly) ||
                    !double.TryParse(values[4], out double target))
                    continue;

                var existing = await _context.CorporateSales
                    .FirstOrDefaultAsync(x =>
                        x.Date == targetDate &&
                        x.CorpType == corpType &&
                        x.Status == Status.Active);

                if (existing != null)
                {
                    // Update existing
                    existing.CY = cy;
                    existing.LY = ly;
                    existing.Target = target;
                    existing.AT = target != 0 ? Math.Round((cy - target) / target * 100, 2) : 0;
                    existing.ALY = Math.Round(cy - ly, 2);
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = currentUserName ?? "SYSTEM_API";
                    updatedCount++;
                }
                else
                {
                    // Create new
                    var newRecord = new CorporateSales
                    {
                        Date = targetDate,
                        CorpType = corpType,
                        CY = cy,
                        LY = ly,
                        Target = target,
                        AT = target != 0 ? Math.Round((cy - target) / target * 100, 2) : 0,
                        ALY = Math.Round(cy - ly, 2),
                        Month = monthAbbr,
                        MonthName = monthInfo.FullName,
                        MonthNum = monthInfo.Number,
                        Year = year,
                        Status = Status.Active,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserName ?? "SYSTEM_API",
                        UpdatedAt = null,
                        UpdatedBy = null
                    };

                    added.Add(newRecord);
                }
            }

            if (added.Any())
            {
                await _context.CorporateSales.AddRangeAsync(added);
            }

            if (added.Any() || updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new ImportResult
            {
                Message = "Import completed successfully",
                Added = added.Count,
                Updated = updatedCount,
                TotalProcessed = added.Count + updatedCount
            });
        }

        /// <summary>
        /// Update existing CorporateSales record
        /// </summary>
        [HttpPut("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(long id, [FromBody] CorporateSalesUpdateDto dto)
        {
            var existing = await _context.CorporateSales.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.CY = dto.CY;
            existing.LY = dto.LY;
            existing.Target = dto.Target;
            existing.AT = dto.Target != 0 ? Math.Round((dto.CY - dto.Target) / dto.Target * 100, 2) : 0;
            existing.ALY = Math.Round(dto.CY - dto.LY, 2);

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = await GetCurrentUserNameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Soft delete a corporate sales record
        /// </summary>
        [HttpDelete("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.CorporateSales.FindAsync(id);
            if (record == null)
                return NotFound();

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = await GetCurrentUserNameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helpers
        private async Task<string?> GetCurrentUserNameAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.UserName;
        }
    }

    // DTOs for better API contract
    public class CorporateSalesUpdateDto
    {
        public double CY { get; set; }
        public double LY { get; set; }
        public double Target { get; set; }
    }

    public class ImportResult
    {
        public string Message { get; set; } = default!;
        public int Added { get; set; }
        public int Updated { get; set; }
        public int TotalProcessed { get; set; }
    }
}