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

namespace ADDPerformance.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AddCkController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Note: IWebHostEnvironment removed as it's not used

        public AddCkController(DBContext context, UserManager<IdentityUser> userManager)
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
        /// Get all active ADD_CK records
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ADD_CK>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ADD_CK>>> GetAll()
        {
            var records = await _context.ADD_CK
                .Where(x => x.Status == Status.Active)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return Ok(records);
        }

        /// <summary>
        /// Get single ADD_CK record by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ADD_CK), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ADD_CK>> GetById(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            return record == null ? NotFound() : Ok(record);
        }

        /// <summary>
        /// Upload and process ADD_CK CSV file (upsert: update if exists, insert if new)
        /// Expected CSV format: Date (JAN-2024),CY,LY,Target
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only .csv files are allowed" });

            string? currentUserName = await GetCurrentUsernameAsync();

            var addedRecords = new List<ADD_CK>();
            int updatedCount = 0;

            using var reader = new StreamReader(file.OpenReadStream());
            bool isFirstLine = true;

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                var values = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 4) continue;

                // Parse date - expecting formats like: JAN-2024 or 01-JAN-2024 etc.
                string dateStr = values[0].Trim().ToUpper();
                string[] parts = dateStr.Split('-');

                if (parts.Length < 2) continue;

                string monthAbbr = parts[^2]; // take second last part (handles 01-JAN-2024 too)
                if (!MonthMap.TryGetValue(monthAbbr, out var monthInfo)) continue;

                if (!int.TryParse(parts[^1], out int year)) continue;

                var targetDate = new DateTime(year, monthInfo.Number, 1);

                if (!double.TryParse(values[1], out double cy) ||
                    !double.TryParse(values[2], out double ly) ||
                    !double.TryParse(values[3], out double target))
                    continue;

                var existing = await _context.ADD_CK
                    .FirstOrDefaultAsync(x => x.Date == targetDate && x.Status == Status.Active);

                if (existing != null)
                {
                    // Update existing record
                    existing.CY = cy;
                    existing.LY = ly;
                    existing.Target = target;
                    existing.AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2);
                    existing.ALY = Math.Round(cy - ly, 2);
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = currentUserName ?? "SYSTEM_API";

                    updatedCount++;
                }
                else
                {
                    // Create new record
                    var newRecord = new ADD_CK
                    {
                        Date = targetDate,
                        CY = cy,
                        LY = ly,
                        Target = target,
                        AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2),
                        ALY = Math.Round(cy - ly, 2),
                        Month = monthAbbr,
                        MonthName = monthInfo.FullName,
                        MonthNum = monthInfo.Number,
                        Year = year,
                        Status = Status.Active,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserName ?? "SYSTEM_API"
                    };

                    addedRecords.Add(newRecord);
                }
            }

            if (addedRecords.Any())
            {
                await _context.ADD_CK.AddRangeAsync(addedRecords);
            }

            if (addedRecords.Any() || updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new ImportResult
            {
                Message = "File processed successfully",
                Added = addedRecords.Count,
                Updated = updatedCount,
                TotalProcessed = addedRecords.Count + updatedCount
            });
        }

        /// <summary>
        /// Soft delete an ADD_CK record
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            if (record == null)
                return NotFound();

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = await GetCurrentUsernameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper methods
        private async Task<string?> GetCurrentUsernameAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.UserName;
        }


        // Response model for upload
        public class ImportResult
        {
            public string Message { get; set; } = default!;
            public int Added { get; set; }
            public int Updated { get; set; }
            public int TotalProcessed { get; set; }
        }
    }
}