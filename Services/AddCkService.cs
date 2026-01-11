using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.EntityFrameworkCore;

namespace ADDPerformance.Services
{
    public class AddCkService : IAddCkService
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly Dictionary<string, (int Number, string FullName)> MonthMap = new()
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")},   {"MAY", (5, "May")},     {"JUN", (6, "June")},
            {"JUL", (7, "July")},    {"AUG", (8, "August")},   {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        public AddCkService(DBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<ImportResult> ProcessAddCkCsvAsync(IFormFile file, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return new ImportResult { Message = "No file uploaded", Added = 0, Updated = 0, TotalProcessed = 0 };

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return new ImportResult { Message = "Only .csv files are allowed", Added = 0, Updated = 0, TotalProcessed = 0 };

            string? currentUserName = null;
            if (user?.Identity?.IsAuthenticated ?? false)
            {
                var userId = user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var identityUser = await _userManager.FindByIdAsync(userId);
                    currentUserName = identityUser?.UserName;
                }
            }

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
                    .FirstOrDefaultAsync(x => x.Date == targetDate && x.Status == Status.Active, cancellationToken);

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
                await _context.ADD_CK.AddRangeAsync(addedRecords, cancellationToken);
            }

            if (addedRecords.Any() || updatedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new ImportResult
            {
                Message = "File processed successfully",
                Added = addedRecords.Count,
                Updated = updatedCount,
                TotalProcessed = addedRecords.Count + updatedCount
            };
        }
    }
}
