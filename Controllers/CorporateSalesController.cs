using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // URL will be: api/CorporateSales
    public class CorporateSalesController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CorporateSalesController(UserManager<IdentityUser> userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        private static readonly Dictionary<string, (int Number, string FullName)> MonthMap = new()
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")}, {"MAY", (5, "May")}, {"JUN", (6, "June")},
            {"JUL", (7, "July")}, {"AUG", (8, "August")}, {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        // 1. GET: api/CorporateSales
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.CorporateSales
                .Where(x => x.Status == Status.Active)
                .ToListAsync();
            return Ok(data);
        }

        // 2. GET: api/CorporateSales/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var record = await _context.CorporateSales.FindAsync(id);
            if (record == null) return NotFound();
            return Ok(record);
        }

        // 3. POST: api/CorporateSales/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            using var reader = new StreamReader(file.OpenReadStream());
            var mainList = new List<CorporateSales>();
            bool isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var lineText = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(lineText) || isFirstLine) { isFirstLine = false; continue; }

                var values = lineText.Split(',');
                if (values.Length < 5) continue;

                // Parse Date Logic
                var temp = values[0]; // Format: JAN-2024
                string[] parts = temp.Split('-');
                if (parts.Length < 2) continue;

                var monthAbbr = parts[0].ToUpper();
                if (MonthMap.TryGetValue(monthAbbr, out var monthInfo))
                {
                    DateTime processedDate = new DateTime(int.Parse(parts[1]), monthInfo.Number, 1);

                    var existing = await _context.CorporateSales
                        .FirstOrDefaultAsync(x => x.Date == processedDate && x.CorpType == values[1]);

                    if (existing != null)
                    {
                        existing.CY = Convert.ToDouble(values[2]);
                        existing.LY = Convert.ToDouble(values[3]);
                        existing.Target = Convert.ToDouble(values[4]);
                        existing.AT = existing.Target != 0 ? Math.Round((existing.CY - existing.Target) / existing.Target * 100, 2) : 0;
                        existing.ALY = Math.Round(existing.CY - existing.LY, 2);
                        existing.UpdatedBy = user?.UserName ?? "System";
                        existing.UpdatedAt = DateTime.Now;
                        _context.Update(existing);
                    }
                    else
                    {
                        var newItem = new CorporateSales
                        {
                            Date = processedDate,
                            CorpType = values[1],
                            CY = Convert.ToDouble(values[2]),
                            LY = Convert.ToDouble(values[3]),
                            Target = Convert.ToDouble(values[4]),
                            Month = monthAbbr,
                            Year = processedDate.Year,
                            MonthNum = monthInfo.Number,
                            MonthName = monthInfo.FullName,
                            Status = Status.Active,
                            CreatedBy = user?.UserName ?? "System",
                            CreatedAt = DateTime.Now
                        };
                        newItem.AT = newItem.Target != 0 ? Math.Round((newItem.CY - newItem.Target) / newItem.Target * 100, 2) : 0;
                        newItem.ALY = Math.Round(newItem.CY - newItem.LY, 2);
                        mainList.Add(newItem);
                    }
                }
            }

            if (mainList.Any()) await _context.AddRangeAsync(mainList);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Import successful", count = mainList.Count });
        }

        // 4. PUT: api/CorporateSales/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CorporateSales dto)
        {
            var existing = await _context.CorporateSales.FindAsync(id);
            if (existing == null) return NotFound();

            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

            existing.CY = dto.CY;
            existing.LY = dto.LY;
            existing.Target = dto.Target;
            existing.AT = dto.Target != 0 ? Math.Round((dto.CY - dto.Target) / dto.Target * 100, 2) : 0;
            existing.ALY = Math.Round(dto.CY - dto.LY, 2);
            existing.UpdatedBy = user?.UserName ?? "System";
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. DELETE: api/CorporateSales/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.CorporateSales.FindAsync(id);
            if (record == null) return NotFound();

            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));

            record.Status = Status.Inactive;
            record.UpdatedBy = user?.UserName ?? "System";
            record.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}