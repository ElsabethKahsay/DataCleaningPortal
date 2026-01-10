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
    public class OnlineSalesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OnlineSalesController(UserManager<IdentityUser> userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
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
        public async Task<IActionResult> ProcessOnlineSalesCsv(IFormFile file, string fileType, IdentityUser loggedInUser)
        {
            try
            {
                var mainList = new List<OnlineSales>();
                var updatedList = new List<OnlineSales>();

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

                        // Date Parsing (Expected format: MMM-yyyy or similar)
                        string rawDate = values[0];
                        string[] dateParts = rawDate.Split('-');
                        string monthAbbr = dateParts[0].ToUpper();
                        int year = int.Parse(dateParts[1]);

                        if (monthAbbreviationsToInfo.TryGetValue(monthAbbr, out var monthInfo))
                        {
                            DateTime targetDate = new DateTime(year, monthInfo.Number, 1);

                            double cy = Convert.ToDouble(values[1]);
                            double ly = Convert.ToDouble(values[2]);
                            double target = Convert.ToDouble(values[3]);

                            var existingRecord = await _context.OnlineSales
                                .FirstOrDefaultAsync(x => x.Date == targetDate);

                            if (existingRecord != null)
                            {
                                existingRecord.CYPercent = cy;
                                existingRecord.LYPercent = ly;
                                existingRecord.TargetPercent = target;
                                existingRecord.AT = Math.Round((cy - target) / target * 100, 2);
                                existingRecord.ALY = Math.Round(cy - ly, 2);
                                existingRecord.Total = cy + "%";
                                existingRecord.Status = Status.Active;
                                existingRecord.UpdatedAt = DateTime.Now;
                                existingRecord.UpdatedBy = loggedInUser?.Email ?? "API_System";
                                updatedList.Add(existingRecord);
                            }
                            else
                            {
                                var line = new OnlineSales
                                {
                                    Date = targetDate,
                                    CYPercent = cy,
                                    LYPercent = ly,
                                    TargetPercent = target,
                                    Month = monthAbbr,
                                    Year = year,
                                    MonthNum = monthInfo.Number,
                                    MonthName = monthInfo.FullName,
                                    AT = Math.Round((cy - target) / target * 100, 2),
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
            var sales = await _context.OnlineSales.Where(i => i.Status == Status.Active).ToListAsync();
            return View(sales);
        }

        public IActionResult Template()
        {
            string fileName = "OnlineSales upload Template.csv";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", fileName);
            if (System.IO.File.Exists(filePath))
            {
                return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", fileName);
            }
            return NotFound();
        }
    }
}