using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IO;

namespace ADDPerformance.Controllers
{
    public class REV_USDController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public REV_USDController(UserManager<IdentityUser> userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        private static readonly Dictionary<string, (int Number, string FullName)> monthAbbreviationsToInfo = new Dictionary<string, (int, string)>
        {
            {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
            {"APR", (4, "April")}, {"MAY", (5, "May")}, {"JUN", (6, "June")},
            {"JUL", (7, "July")}, {"AUG", (8, "August")}, {"SEP", (9, "September")},
            {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
        };

        // --- API COMPATIBLE ENGINE ---
        [NonAction]
        public async Task<IActionResult> ProcessRevUsdCsv(IFormFile file, string fileType, IdentityUser loggedInUser)
        {
            try
            {
                var mainList = new List<REV_USD>();
                var updatedList = new List<REV_USD>();

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    bool isFirstLine = true;

                    while (!reader.EndOfStream)
                    {
                        var data = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(data)) continue;

                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            continue;
                        }

                        var values = data.Split(',');
                        if (values.Length < 4) continue;

                        // Parse Date (Expected format: JAN-2024 or 01-JAN-2024)
                        string rawDate = values[0];
                        string[] dateParts = rawDate.Split('-');

                        // Handle potential date format variations (Month vs Year index)
                        string monthAbbr = dateParts.Length > 1 ? dateParts[0].ToUpper() : "";
                        int year = dateParts.Length > 1 ? int.Parse(dateParts[1]) : DateTime.Now.Year;

                        if (monthAbbreviationsToInfo.TryGetValue(monthAbbr, out var monthInfo))
                        {
                            DateTime targetDate = new DateTime(year, monthInfo.Number, 1);

                            double cy = Convert.ToDouble(values[1]);
                            double ly = Convert.ToDouble(values[2]);
                            double target = Convert.ToDouble(values[3]);

                            var existingRecord = await _context.REV_USD
                                .FirstOrDefaultAsync(x => x.Date == targetDate);

                            if (existingRecord != null)
                            {
                                existingRecord.CY_USD = cy;
                                existingRecord.LY_USD = ly;
                                existingRecord.Target_USD = target;
                                existingRecord.AT = target == 0 ? 0 : Math.Round((cy - target) / target * 100, 2);
                                existingRecord.ALY = Math.Round(cy - ly, 2);
                                existingRecord.Total = cy + "%";
                                existingRecord.Status = Status.Active;
                                existingRecord.UpdatedAt = DateTime.Now;
                                existingRecord.UpdatedBy = loggedInUser?.Email ?? "API_System";
                                updatedList.Add(existingRecord);
                            }
                            else
                            {
                                var line = new REV_USD
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
                                    Total = cy + "%",
                                    Status = Status.Active,
                                    CreatedAt = DateTime.Now,
                                    CreatedBy = loggedInUser?.UserName ?? "API_System"
                                };
                                mainList.Add(line);
                            }
                        }
                    }
                }

                if (updatedList.Any()) _context.UpdateRange(updatedList);
                if (mainList.Any()) _context.AddRange(mainList);

                await _context.SaveChangesAsync();
                return new OkObjectResult(new { status = "Success", added = mainList.Count, updated = updatedList.Count });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { status = "Error", message = ex.Message });
            }
        }

        // --- EXISTING MVC ACTIONS ---
        public async Task<IActionResult> Index()
        {
            if (_context.REV_USD == null) return Problem("Entity set is null.");
            return View(await _context.REV_USD.Where(i => i.Status == Status.Active).ToListAsync());
        }

        public IActionResult Template()
        {
            string fileName = "Rev upload Template.csv";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", fileName);
            if (System.IO.File.Exists(filePath))
            {
                return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", fileName);
            }
            return NotFound();
        }

        // ... Details, Create, Edit, Delete methods omitted for brevity, keep your original ones below if needed ...
    }
}