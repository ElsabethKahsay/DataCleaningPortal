using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using ADDPerformance.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Data;

namespace ADDPerformance.Controllers
{
    public class DestinationsController : Controller
    {
        public UserManager<IdentityUser> userManager { get; private set; }
        private readonly DBContext _context;
        private readonly ReadFromTable readFromTable;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DestinationsController(UserManager<IdentityUser> _userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            this.userManager = _userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            readFromTable = new ReadFromTable(context);
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
        public async Task<IActionResult> ProcessDestinationsCsv(IFormFile file, string fileType, IdentityUser loggedInUser)
        {
            try
            {
                DataTable CityTable = readFromTable.ReadFrometfpr(Keys.Query);
                var mainList = new List<Destinations>();
                var updatedList = new List<Destinations>();

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    int lineNumber = 0;
                    string[] headerValues = null;

                    while (!reader.EndOfStream)
                    {
                        var data = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(data)) continue;

                        var values = data.Split(',');

                        if (lineNumber == 0)
                        {
                            headerValues = values;
                            lineNumber++;
                            continue;
                        }

                        string tempDest = values[0];
                        string tempOrigin = values[1];
                        DataRow[] destRows = CityTable.Select($"[IATA_Code] = '{tempDest}'");
                        DataRow[] originRows = CityTable.Select($"[IATA_Code] = '{tempOrigin}'");

                        string destinationCity = destRows.Length > 0 ? destRows[0]["City"].ToString() : "";
                        string originCity = originRows.Length > 0 ? originRows[0]["City"].ToString() : "";

                        // Loop through month columns starting from index 2
                        for (int i = 2; i < headerValues.Length; i++)
                        {
                            string header = headerValues[i];
                            if (string.IsNullOrEmpty(header) || !header.Contains("-")) continue;

                            string[] parts = header.Split('-');
                            string monthAbbr = parts[0].ToUpper();
                            if (!int.TryParse(parts[1], out int year)) continue;

                            if (monthAbbreviationsToInfo.TryGetValue(monthAbbr, out var monthInfo))
                            {
                                DateTime dateOnly = new DateTime(year, monthInfo.Number, 1);
                                string valueToConvert = values.Length > i ? values[i] : "0";
                                long pax = (string.IsNullOrEmpty(valueToConvert) || valueToConvert.Contains("-")) ? 0 : Convert.ToInt64(valueToConvert);

                                var existingRecord = await _context.Destinations
                                    .FirstOrDefaultAsync(x => x.Destination == tempDest && x.Month == dateOnly && x.Origin == tempOrigin);

                                if (existingRecord != null)
                                {
                                    existingRecord.paxCount = pax;
                                    existingRecord.UpdatedAt = DateTime.Now;
                                    existingRecord.UpdatedBy = loggedInUser?.Email ?? "API_System";
                                    updatedList.Add(existingRecord);
                                }
                                else
                                {
                                    mainList.Add(new Destinations
                                    {
                                        Destination = tempDest,
                                        Origin = tempOrigin,
                                        OriginCity = originCity,
                                        DestCity = destinationCity,
                                        paxCount = pax,
                                        MonthNum = monthInfo.Number,
                                        MonthName = monthInfo.FullName,
                                        Year = year,
                                        Month = dateOnly,
                                        Status = Status.Active,
                                        CreatedAt = DateTime.Now,
                                        CreatedBy = loggedInUser?.UserName ?? "API_System"
                                    });
                                }
                            }
                        }
                    }
                }

                if (updatedList.Any()) _context.UpdateRange(updatedList);
                if (mainList.Any()) _context.AddRange(mainList);

                await _context.SaveChangesAsync();
                return new OkObjectResult(new
                {
                    status = "Success",
                    message = "File processed successfully",
                    added = mainList.Count,
                    updated = updatedList.Count
                });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { status = "Error", message = ex.Message });
            }
        }

        // --- EXISTING MVC ACTIONS ---
        public async Task<IActionResult> Index()
        {
            return View(await _context.Destinations.Where(i => i.Status == Status.Active).ToListAsync());
        }

        public IActionResult Template()
        {
            string fileName = "Dest upload Template.csv";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "FileStore", fileName);
            if (System.IO.File.Exists(filePath))
            {
                return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", fileName);
            }
            return NotFound();
        }
    }
}