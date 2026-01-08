//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using ADDPerformance.Data;
//using ADDPerformance.Models;
//using ADDPerformance.Helpers;
//using Microsoft.AspNetCore.Identity;
//using System.Security.Claims;
//using System.Data;
//using Microsoft.IdentityModel.Tokens;
//using System.Drawing;


//namespace ADDPerformance.Controllers
//{
//    public class DestinationsController : Controller
//    {
//        public UserManager<IdentityUser> userManager { get; private set; }
//        private readonly DBContext _context;
//        ReadFromTable readFromTable;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        public DestinationsController(UserManager<IdentityUser> _userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
//        {
//            this.userManager = _userManager;
//            readFromTable = new ReadFromTable(context);
//            _webHostEnvironment = webHostEnvironment;
//            _context = context;
//        }

//        Dictionary<string, (int Number, string FullName)> monthAbbreviationsToInfo = new Dictionary<string, (int, string)>
//{
//    {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
//    {"APR", (4, "April")}, {"MAY", (5, "May")}, {"JUN", (6, "June")},
//    {"JUL", (7, "July")}, {"AUG", (8, "August")}, {"SEP", (9, "September")},
//    {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
//};


//        // GET: Destinations
//        public async Task<IActionResult> Index()
//        {
//            return _context.Destinations != null ?
//                        View(await _context.Destinations.Where(i => i.Status == Status.Active).ToListAsync()) :
//                        Problem("Entity set 'DBContext.Destinations'  is null.");
//        }
//        public IActionResult Template()
//        {
//            string fileName = "Dest upload Template.csv";
//            // Define the path to the Templates folder in the root directory
//            var templatesFolder = Path.Combine(Directory.GetCurrentDirectory(), "FileStore");

//            // Combine the folder path and the requested file name
//            var filePath = Path.Combine(templatesFolder, fileName);

//            // Check if the file exists
//            if (System.IO.File.Exists(filePath))
//            {
//                // Read the file content and determine its content type
//                var fileBytes = System.IO.File.ReadAllBytes(filePath);
//                var contentType = "application/octet-stream"; // Binary file content type

//                // Return the file for download with a suggested file name
//                return File(fileBytes, contentType, fileName);
//            }
//            else
//            {
//                // If the file doesn't exist, return a NotFound response
//                return NotFound();
//            }
//        }


//        public async Task<IActionResult> Upload()
//        {
//            // Retrieve uploaded files and form fields
//            string url = string.Empty;
//            var attachedFile = Request.Form.Files;
//            var filtType = Request.Form["FileType"];
//            // user identity for audit attribute
//            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//            // Check if at least one file is attached
//            if (attachedFile.Count > 0)
//            {
//                // Get information about the uploaded file
//                var fileExt = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.LastOrDefault();
//                var fileName = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.FirstOrDefault();
//                var contentType = attachedFile?.FirstOrDefault()?.ContentType;
//                var size = attachedFile?.FirstOrDefault()?.Length;
//                var file = attachedFile?[0];

//                // Check if the uploaded file has a CSV extension
//                if (fileExt.Equals("csv", StringComparison.OrdinalIgnoreCase))
//                {
//                    using (var reader = new StreamReader(file.OpenReadStream()))
//                    {
//                        DataTable CityTable = readFromTable.ReadFrometfpr(Keys.Query);
//                        var mainList = new List<Destinations>();
//                        var UpdatedList = new List<Destinations>();
//                        int lineNumber = 0;
//                        Dictionary<long, string> paxSums = new Dictionary<long, string>();
//                        string[] headerValues = null;
//                        while (!reader.EndOfStream)
//                        {
//                            var data = reader.ReadLine();
//                            var values = data?.Split(',');

//                            if (data != null)
//                            {
//                                string tempDest = string.Empty;
//                                tempDest = values[0];
//                                string tempOrign = string.Empty;
//                                tempOrign = values[1];
//                                DataRow[] destRows = CityTable.Select("[IATA_Code] = '" + tempDest + "'");
//                                DataRow[] originRows = CityTable.Select("[IATA_Code] = '" + tempOrign + "'");
//                                string destinationCity = string.Empty;
//                                string originCity = string.Empty;
//                                if (destRows.Length > 0 && originRows.Length > 0 && data != string.Empty)
//                                {
//                                    destinationCity = destRows[0]["City"].ToString();
//                                    originCity = originRows[0]["City"].ToString();
//                                }

//                                if (lineNumber == 0)
//                                {
//                                    //headerLine = reader.ReadLine();
//                                    headerValues = data.Split(',');
//                                }
//                                else
//                                {

//                                    //  string city = string.Empty;
//                                    for (int i = 2; i < headerValues.Length - 1; i++)
//                                    {
//                                        string temp = Convert.ToString(headerValues[i]);
//                                        string[] parts = temp.Split('-');

//                                        string monthAbbreviation = parts[0].ToUpper(); // Ensure uppercase for case-insensitive lookup
//                                        int year = int.Parse(parts[1]);

//                                        if (monthAbbreviationsToInfo.TryGetValue(monthAbbreviation, out var monthInfo))
//                                        {
//                                            //  DateTime dateOnly = new DateTime(year, monthInfo.Number, 1);
//                                        }
//                                        var MonthNumber = monthInfo.Number;
//                                        DateTime dateOnly = new DateTime(year, MonthNumber, 1);
//                                        Destinations line = new()
//                                        {
//                                            Destination = Convert.ToString(values[0]),
//                                            Origin = Convert.ToString(values[1]),
//                                        };

//                                        // check if the read value exists
//                                        //if so update the record
//                                        if (_context.Destinations.Where(x => x.Destination == line.Destination && x.Month == dateOnly).Any())
//                                        {
//                                            var existingRecord = _context.Destinations.FirstOrDefault(x => x.Destination == line.Destination && x.Month == dateOnly);

//                                            existingRecord.MonthNum = monthInfo.Number;
//                                            existingRecord.MonthName = monthInfo.FullName;
//                                            existingRecord.Year = year;
//                                            existingRecord.Month = new DateTime(year, monthInfo.Number, 1);
//                                            string valueToConvert = values[i];

//                                            //validate paxCount
//                                            if (string.IsNullOrEmpty(valueToConvert) || valueToConvert.Contains("-"))
//                                            {
//                                                existingRecord.paxCount = 0;
//                                            }
//                                            else
//                                            {
//                                                existingRecord.paxCount = Convert.ToInt64(valueToConvert);

//                                            }
//                                            existingRecord.UpdatedAt = System.DateTime.Now;
//                                            existingRecord.UpdatedBy = loggedInUser.Email;
//                                            UpdatedList.Add(existingRecord);
//                                            _context.UpdateRange(UpdatedList);
//                                        }

//                                        else
//                                        {
//                                            line.OriginCity = originCity;
//                                            line.DestCity = destinationCity;
//                                            string valueToConvert = values[i];

//                                            if (string.IsNullOrEmpty(valueToConvert) || valueToConvert.Contains("-"))
//                                            {
//                                                line.paxCount = 0;
//                                            }
//                                            else
//                                            {
//                                                line.paxCount = Convert.ToInt64(valueToConvert);
//                                            }
//                                            line.MonthNum = monthInfo.Number;
//                                            line.Month = dateOnly;
//                                            line.Year = year;
//                                            line.Status = Status.Active;
//                                            line.MonthName = monthInfo.FullName;
//                                            line.CreatedAt = DateTime.Now; ;
//                                            line.CreatedBy = loggedInUser.UserName;
//                                            mainList.Add(line);
//                                        }
//                                    }
//                                }
//                                lineNumber = +1;
//                            }
//                        }
//                        _context.AddRange(mainList);
//                        await _context.SaveChangesAsync();
//                        TempData["SuccessMessage"] = "File successfully imported.";
//                        return RedirectToAction(nameof(Index));
//                    }
//                }
//                else
//                {
//                    TempData["FailureAlertMessage"] = "Please choose a proper file format to import";
//                }
//            }
//            else
//            {
//                TempData["FailureAlertMessage"] = "Please choose a  file to import";
//            }

//            return View(nameof(Index));
//        }

//        // GET: Destinations/Details/5
//        public async Task<IActionResult> Details(long? id)
//        {
//            if (id == null || _context.Destinations == null)
//            {
//                return NotFound();
//            }

//            var destinations = await _context.Destinations
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (destinations == null)
//            {
//                return NotFound();
//            }

//            return View(destinations);
//        }

//        // GET: Destinations/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: Destinations/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Destinations destinations)
//        {
//            if (ModelState.IsValid)
//            {
//                DateTime inputDate = destinations.Month;

//                int year = inputDate.Year;

//                int monthNum = inputDate.Month;
//                string monthName = inputDate.ToString("MMMM"); // Full month name
//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                DateTime newDate = new DateTime(year, monthNum, 1);

//                if (!_context.Destinations.Where(x => x.Destination == destinations.Destination && x.Month == newDate).Any())
//                {
//                    DataTable CityTable = readFromTable.ReadFrometfpr(Keys.Query);
//                    var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                    var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);

//                    destinations.CreatedAt = DateTime.Now;
//                    destinations.CreatedBy = loggedInUser.UserName;
//                    destinations.Status = Status.Active;
//                    destinations.Month = newDate;
//                    destinations.Year = year;
//                    destinations.MonthNum = monthNum;
//                    destinations.MonthName = monthName;
//                    destinations.paxCount = Convert.ToInt64(destinations.paxCount);
//                    // fetch destination city and origin city from their respective datatable
//                    DataRow[] destRows = CityTable.Select("[IATA_Code] = '" + destinations.Destination + "'");
//                    DataRow[] originRows = CityTable.Select("[IATA_Code] = '" + destinations.Origin + "'");
//                    if (destRows != null && originRows != null)
//                    {
//                        try
//                        {
//                            destinations.DestCity = destRows[0]["City"].ToString();
//                            destinations.OriginCity = originRows[0]["City"].ToString();
//                        }
//                        catch
//                        {
//                            TempData["FailureAlertMessage"] = "Could't find the destination or origin city or both. please try again";
//                            return RedirectToAction(nameof(Index));
//                        }
//                    }
//                    destinations.Destination = destinations.Destination.ToString();
//                    destinations.Origin = destinations.Origin.ToString();
//                }
//                else
//                {
//                    TempData["FailureAlertMessage"] = "Data already exists. Please try again.";
//                    return View();
//                }

//                _context.AddRange(destinations);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            else
//            {
//                TempData["FailureAlertMessage"] = "Please try again.";
//                return View(destinations);
//            }
//        }

//        // GET: Destinations/Edit/5
//        public async Task<IActionResult> Edit(long? id)
//        {
//            if (id == null || _context.Destinations == null)
//            {
//                return NotFound();
//            }

//            var destinations = await _context.Destinations.FindAsync(id);
//            if (destinations == null)
//            {
//                return NotFound();
//            }
//            return View(destinations);
//        }

//        // POST: Destinations/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(long id, Destinations destinations)
//        {
//            if (id != destinations.Id)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {

//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//                DateTime inputDate = destinations.Month;

//                int year = inputDate.Year;
//                int monthNum = inputDate.Month;

//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                DateTime newDate = new DateTime(year, monthNum, 1); // assigns 1 to the date part for consistency
//                string monthName = inputDate.ToString("MMMM"); // Full month name

//                var previous_date = _context.Destinations.Where(x => x.Id == id).FirstOrDefault();
//                if (destinations.Month == previous_date.Month || !_context.Destinations.Where(x => x.Month == inputDate).Any())
//                {
//                    try
//                    {
//                        _context.Entry(previous_date).State = EntityState.Detached;

//                        DataTable CityTable = readFromTable.ReadFrometfpr(Keys.Query);
//                        destinations.UpdatedBy = loggedInUser.Email;
//                        destinations.UpdatedAt = DateTime.Now;
//                        destinations.Status = Status.Active;
//                        destinations.Month = newDate;
//                        destinations.Year = year;
//                        destinations.MonthNum = monthNum;
//                        destinations.MonthName = monthName;
//                        destinations.paxCount = Convert.ToInt64(destinations.paxCount);
//                        DataRow[] destRows = CityTable.Select("[IATA_Code] = '" + destinations.Destination + "'");
//                        DataRow[] originRows = CityTable.Select("[IATA_Code] = '" + destinations.Origin + "'");

//                        // check if the datatable returned something
//                        if (destRows != null && originRows != null)
//                        {
//                            try
//                            {
//                                destinations.DestCity = destRows[0]["City"].ToString();
//                                destinations.OriginCity = originRows[0]["City"].ToString();
//                            }
//                            catch
//                            {
//                                TempData["FailureAlertMessage"] = "Could't find the destination or origin city or both. please try again";
//                                return RedirectToAction(nameof(Index));
//                            }
//                        }

//                        destinations.Destination = destinations.Destination.ToString();
//                        destinations.Origin = destinations.Origin.ToString();

//                        _context.Update(destinations);
//                        await _context.SaveChangesAsync();
//                    }
//                    catch (DbUpdateConcurrencyException)
//                    {
//                        TempData["FailureAlertMessage"] = "Something is wrong. Please try again.";
//                        return View(destinations);
//                    }
//                }
//                else
//                {
//                    TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
//                    return View(destinations);
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            return View(destinations);
//        }

//        // GET: Destinations/Delete/5
//        public async Task<IActionResult> Delete(long? id)
//        {
//            if (id == null || _context.Destinations == null)
//            {
//                return NotFound();
//            }

//            var destinations = await _context.Destinations
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (destinations == null)
//            {
//                return NotFound();
//            }

//            return View(destinations);
//        }

//        // POST: Destinations/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(long id)
//        {
//            if (_context.Destinations == null)
//            {
//                return Problem("Entity set 'DBContext.Destinations'  is null.");
//            }
//            var destinations = await _context.Destinations.FindAsync(id);
//            if (destinations != null)
//            {

//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//                destinations.UpdatedAt = DateTime.UtcNow;
//                destinations.UpdatedBy = loggedInUser.UserName;
//                destinations.Status = Status.Inactive;
//                await _context.SaveChangesAsync();
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool DestinationsExists(long id)
//        {
//            return (_context.Destinations?.Any(e => e.Id == id)).GetValueOrDefault();
//        }
//    }
//}
