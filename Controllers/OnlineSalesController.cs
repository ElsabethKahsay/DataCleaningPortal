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
using Microsoft.AspNetCore.Hosting;

namespace ADDPerformance.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires JWT Token for all actions
    public class OnlineSalesController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;

        public OnlineSalesController(UserManager<IdentityUser> userManager, DBContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        private static readonly Dictionary<string, (int Number, string FullName)> MonthInfo = new()
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")},   {"MAY", (5, "May")},     {"JUN", (6, "June")},
            {"JUL", (7, "July")},    {"AUG", (8, "August")},   {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        // 1. GET: api/OnlineSales (Get All)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OnlineSales>>> GetAll()
        {
            return await _context.OnlineSales
                .Where(x => x.Status == Status.Active)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        // 2. GET: api/OnlineSales/5 (Details)
        [HttpGet("{id}")]
        public async Task<ActionResult<OnlineSales>> GetDetails(int id)
        {
            var record = await _context.OnlineSales.FindAsync(id);
            if (record == null) return NotFound();
            return record;
        }

        // 3. POST: api/OnlineSales (Create)
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<OnlineSales>> Create(OnlineSales record)
        {
            var user = await _userManager.GetUserAsync(User);

            record.CreatedAt = DateTime.Now;
            record.CreatedBy = user?.UserName ?? "API_System";
            record.Status = Status.Active;

            // Auto-calculate performance fields
            if (record.TargetPercent != 0)
                record.AT = Math.Round((record.CYPercent - record.TargetPercent) / record.TargetPercent * 100, 2);

            record.ALY = Math.Round(record.CYPercent - record.LYPercent, 2);
            record.Total = record.CYPercent + "%";

            _context.OnlineSales.Add(record);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetails), new { id = record.Id }, record);
        }

        // 4. PUT: api/OnlineSales/5 (Edit)
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, OnlineSales record)
        {
            if (id != record.Id) return BadRequest("ID Mismatch");

            var existing = await _context.OnlineSales.FindAsync(id);
            if (existing == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Update fields
            existing.CYPercent = record.CYPercent;
            existing.LYPercent = record.LYPercent;
            existing.TargetPercent = record.TargetPercent;
            existing.Date = record.Date;

            // Recalculate
            existing.AT = existing.TargetPercent == 0 ? 0 : Math.Round((existing.CYPercent - existing.TargetPercent) / existing.TargetPercent * 100, 2);
            existing.ALY = Math.Round(existing.CYPercent - existing.LYPercent, 2);
            existing.Total = existing.CYPercent + "%";

            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = user?.Email ?? "API_System";

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.OnlineSales.Any(e => e.Id == id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/OnlineSales/5
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.OnlineSales.FindAsync(id);
            if (record == null) return NotFound();

            _context.OnlineSales.Remove(record);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 6. POST: api/OnlineSales/upload
        [HttpPost("upload")]
       // [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var user = await _userManager.GetUserAsync(User);
            var mainList = new List<OnlineSales>();
            var updatedList = new List<OnlineSales>();

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                bool isFirstLine = true;

                while (!reader.EndOfStream)
                {
                    var data = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(data) || isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var values = data.Split(',');
                    if (values.Length < 4) continue;

                    // Date Parsing
                    string[] dateParts = values[0].Split('-');
                    string monthAbbr = dateParts[0].ToUpper();
                    if (!int.TryParse(dateParts[1], out int year) || !MonthInfo.TryGetValue(monthAbbr, out var m)) continue;

                    DateTime targetDate = new DateTime(year, m.Number, 1);
                    double cy = Convert.ToDouble(values[1]);
                    double ly = Convert.ToDouble(values[2]);
                    double target = Convert.ToDouble(values[3]);

                    var existing = await _context.OnlineSales.FirstOrDefaultAsync(x => x.Date == targetDate);

                    if (existing != null)
                    {
                        existing.CYPercent = cy;
                        existing.LYPercent = ly;
                        existing.TargetPercent = target;
                        existing.AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2);
                        existing.ALY = Math.Round(cy - ly, 2);
                        existing.UpdatedAt = DateTime.Now;
                        existing.UpdatedBy = user?.Email ?? "API_System";
                        updatedList.Add(existing);
                    }
                    else
                    {
                        mainList.Add(new OnlineSales
                        {
                            Date = targetDate,
                            CYPercent = cy,
                            LYPercent = ly,
                            TargetPercent = target,
                            Month = monthAbbr,
                            Year = year,
                            MonthNum = m.Number,
                            MonthName = m.FullName,
                            AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2),
                            ALY = Math.Round(cy - ly, 2),
                            Total = cy + "%",
                            Status = Status.Active,
                            CreatedAt = DateTime.Now,
                            CreatedBy = user?.UserName ?? "API_System"
                        });
                    }
                }
                if (updatedList.Any()) _context.UpdateRange(updatedList);
                if (mainList.Any()) _context.AddRange(mainList);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", added = mainList.Count, updated = updatedList.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "Error", message = ex.Message });
            }
        }
    }
}