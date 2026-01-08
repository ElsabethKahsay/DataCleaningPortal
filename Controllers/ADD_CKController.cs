using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Collections.Generic;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ADD_CKController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ADD_CKController(UserManager<IdentityUser> userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        private readonly Dictionary<string, (int Number, string FullName)> monthAbbreviationsToInfo = new()
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")}, {"MAY", (5, "May")}, {"JUN", (6, "June")},
            {"JUL", (7, "July")}, {"AUG", (8, "August")}, {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        // 1. GET: api/ADD_CK
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ADD_CK>>> GetList()
        {
            var list = await _context.ADD_CK
                .Where(i => i.Status == Status.Active)
                .ToListAsync();

            return Ok(list);
        }

        // 2. POST: api/ADD_CK/upload
        [HttpPost("upload")]
        public async Task<IActionResult> ProcessAddCkCsv(IFormFile file, List<>
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            loggedInUser)
        {
            ArgumentNullException.ThrowIfNull(loggedInUser);
            using var reader = new StreamReader(file.OpenReadStream());
            var dateList = new List<DateMaster>();
            var mainList = new List<ADD_CK>();
            var isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var data = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(data)) continue;

                if (isFirstLine) { isFirstLine = false; continue; }

                var values = data.Split(',');
                var temp = values[0];
                var tempDate = Convert.ToDateTime(values[0]);
                string[] tempparts = temp.Split('-');

                var monthAbbr = temp.Substring(0, 3).ToUpper();
                var year = int.Parse(tempparts[1]);

                if (monthAbbreviationsToInfo.TryGetValue(monthAbbr, out var info))
                {
                    tempDate = new DateTime(year, info.Number, 1);
                }

                // Logic: Update or Create
                var existingRecord = await _context.ADD_CK.FirstOrDefaultAsync(x => x.Date == tempDate);
                if (existingRecord != null)
                {
                    existingRecord.CY = Convert.ToDouble(values[1]);
                    existingRecord.LY = Convert.ToDouble(values[2]);
                    existingRecord.Target = Convert.ToDouble(values[3]);
                    existingRecord.AT = Math.Round((existingRecord.CY - existingRecord.Target) / existingRecord.Target * 100, 2);
                    existingRecord.ALY = Math.Round(existingRecord.CY - existingRecord.LY, 2);
                    existingRecord.UpdatedAt = DateTime.Now;
                    existingRecord.UpdatedBy = loggedInUser?.Email ?? "System";
                    _context.Update(existingRecord);
                }
                else
                {
                    ADD_CK line = new()
                    {
                        CY = Convert.ToDouble(values[1]),
                        LY = Convert.ToDouble(values[2]),
                        Target = Convert.ToDouble(values[3]),
                        Month = monthAbbr,
                        Year = year,
                        MonthNum = info.Number,
                        MonthName = info.FullName,
                        Date = tempDate,
                        Status = Status.Active,
                        CreatedAt = DateTime.Now,
                        CreatedBy = loggedInUser?.UserName ?? "System"
                    };
                    line.AT = line.Target == 0 ? 0 : Math.Round((line.CY - line.Target) / line.Target * 100, 2);
                    line.ALY = Math.Round(line.CY - line.LY, 2);

                    mainList.Add(line);
                }
            }

            await _context.AddRangeAsync(mainList);
            await _context.SaveChangesAsync();

            return Ok(new { message = "File successfully processed." });
        }

        // 3. GET: api/ADD_CK/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ADD_CK>> GetDetails(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            if (record == null) return NotFound();
            return Ok(record);
        }

        // 4. DELETE: api/ADD_CK/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            if (record == null) return NotFound();

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}