//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using ADDPerformance.Data;
//using ADDPerformance.Models;
//using Microsoft.AspNetCore.Identity;
//using System.Security.Claims;

//namespace ADDPerformance.Controllers
//{
//    public class REV_USDController : Controller
//    {
//        public UserManager<IdentityUser> userManager { get; private set; }
//        private readonly DBContext _context;
//        private readonly IWebHostEnvironment _webHostEnvironment;

//        public REV_USDController(UserManager<IdentityUser> _userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
//        {
//            this.userManager = _userManager;
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
//        public IActionResult Template()
//        {
//            string fileName = "Rev upload Template.csv";
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

//            if (attachedFile.Count > 0)
//            {
//                // Get the file extension and other information about the uploaded file
//                var fileExt = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.LastOrDefault();
//                var fileName = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.FirstOrDefault();
//                var contentType = attachedFile?.FirstOrDefault()?.ContentType;
//                var size = attachedFile?.FirstOrDefault()?.Length;
//                var file = attachedFile?[0];

//                // Check if the file is a CSV  file
//                if (fileExt.Equals("csv", StringComparison.OrdinalIgnoreCase))
//                {
//                    // Read the CSV file
//                    using (var reader = new StreamReader(file.OpenReadStream()))
//                    {
//                        var mainList = new List<REV_USD>();
//                        var UpdatedList = new List<REV_USD>();
//                        var isFirstLine = true; // Flag to identify the first line

//                        // Read the CSV file line by line
//                        while (!reader.EndOfStream)
//                        {
//                            var data = reader.ReadLine();
//                            // Check if the line is not empty
//                            if (data != string.Empty)
//                            {
//                                if (isFirstLine)
//                                {
//                                    isFirstLine = false;
//                                    continue; // Skip processing the first line
//                                }
//                                else
//                                {
//                                    var values = data?.Split(',');
//                                    // Extract and convert date from the CSV to string to apply the split() function
//                                    var temp = Convert.ToString(values[0]);

//                                    var tempDate = Convert.ToDateTime(values[0]);
//                                    string[] tempparts = temp.Split('-');

//                                    // Extract date parts for processing

//                                    var Month = tempparts[1];// Extract the three-letter month abbreviation
//                                    var Year = int.Parse(tempparts[1]);

//                                    if (monthAbbreviationsToInfo.TryGetValue(Month.ToUpper(), out var tempmonthInfo))
//                                    {
//                                        DateTime DateOnly = new DateTime(Year, tempmonthInfo.Number, 1);
//                                        tempDate = DateOnly;
//                                    }

//                                    // Check if a record with the same date already exists in the database
//                                    if (_context.REV_USD.Where(x => x.Date == tempDate).Any())
//                                    {
//                                        // Update an existing record
//                                        var existingRecord = _context.REV_USD.FirstOrDefault(x => x.Date == tempDate);
//                                        existingRecord.CY_USD = Convert.ToDouble(values[1]);
//                                        existingRecord.LY_USD = Convert.ToDouble(values[2]);
//                                        existingRecord.Target_USD = Convert.ToDouble(values[3]);
//                                        if (existingRecord.Target_USD == 0)
//                                        {
//                                            existingRecord.AT = 0;
//                                        }
//                                        else
//                                        {
//                                            existingRecord.AT = Math.Round((existingRecord.CY_USD - existingRecord.Target_USD) / existingRecord.Target_USD * 100, 2);
//                                        }

//                                        existingRecord.ALY = Math.Round(existingRecord.CY_USD - existingRecord.LY_USD, 2);
//                                        existingRecord.Total = existingRecord.CY_USD + "%";
//                                        existingRecord.UpdatedAt = System.DateTime.Now;
//                                        existingRecord.UpdatedBy = loggedInUser.Email;
//                                        UpdatedList.Add(existingRecord);
//                                        _context.UpdateRange(UpdatedList);
//                                    }
//                                    else
//                                    {
//                                        // Create a new REV_USD object and store data
//                                        REV_USD line = new()
//                                        {
//                                            Date = Convert.ToDateTime(values[0]),
//                                            CY_USD = Convert.ToDouble(values[1]),
//                                            LY_USD = Convert.ToDouble(values[2]),
//                                            Target_USD = Convert.ToDouble(values[3]),
//                                        };

//                                        // Parse date-related information using the temp variable
//                                        line.Month = temp.Substring(0, 3);
//                                        line.Year = Year;
//                                        if (monthAbbreviationsToInfo.TryGetValue(line.Month.ToUpper(), out var monthInfo))
//                                        {
//                                            line.MonthNum = monthInfo.Number;
//                                            line.Date = new DateTime(line.Year, monthInfo.Number, 1); ; // to make sure the date is always 1
//                                            line.MonthName = monthInfo.FullName;
//                                        }
//                                        //againest target percentage
//                                        line.AT = Math.Round((line.CY_USD - line.Target_USD) / line.Target_USD * 100, 2);
//                                        //againest last year 
//                                        line.ALY = Math.Round(line.CY_USD - line.LY_USD, 2);
//                                        line.Total = line.CY_USD + "%";
//                                        line.Status = Status.Active;

//                                        //audit trail
//                                        line.CreatedAt = DateTime.Now;
//                                        line.CreatedBy = loggedInUser.UserName;
//                                        _context.REV_USD.Add(line);
//                                    }
//                                }
//                            }
//                        }
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
//        // GET: REV_USD
//        public async Task<IActionResult> Index()
//        {
//            return _context.REV_USD != null ?
//                        View(await _context.REV_USD.Where(i => i.Status == Status.Active).ToListAsync()) :
//                        Problem("Entity set 'DBContext.REV_USD'  is null.");
//        }

//        // GET: REV_USD/Details/5
//        public async Task<IActionResult> Details(long? id)
//        {
//            if (id == null || _context.REV_USD == null)
//            {
//                return NotFound();
//            }

//            var rEV_USD = await _context.REV_USD
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (rEV_USD == null)
//            {
//                return NotFound();
//            }

//            return View(rEV_USD);
//        }

//        // GET: REV_USD/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: REV_USD/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(REV_USD rEV_USD)
//        {
//            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//            if (ModelState.IsValid)
//            {
//                // Extract and convert date from the CSV to string to apply the split() 
//                DateTime inputDate = rEV_USD.Date;

//                int year = inputDate.Year;
//                int monthNum = inputDate.Month;
//                string monthName = inputDate.ToString("MMMM"); // Full month name
//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                                                                      // DateTime newDate = ; // assigns 1 to the date part for consistency

//                if (_context.REV_USD.Where(x => x.Date == inputDate).Any())
//                {
//                    TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
//                }
//                else
//                {
//                    try
//                    {
//                        rEV_USD.LY_USD = Convert.ToDouble(rEV_USD.LY_USD);
//                        rEV_USD.Target_USD = Convert.ToDouble(rEV_USD.Target_USD);
//                        rEV_USD.CY_USD = Convert.ToDouble(rEV_USD.CY_USD);
//                        rEV_USD.Month = monthAbbreviation;
//                        rEV_USD.Year = year;
//                        if (monthAbbreviationsToInfo.TryGetValue(rEV_USD.Month.ToUpper(), out var monthInfo))
//                        {
//                            DateTime DateOnly = new DateTime(rEV_USD.Year, monthInfo.Number, 1);

//                            string[] dateParts = DateOnly.ToString().Split(' ');
//                            rEV_USD.MonthNum = monthNum;
//                            rEV_USD.MonthName = monthName;
//                        }
//                        rEV_USD.Date = new DateTime(year, monthNum, 1);
//                        // againest target percentage
//                        if (rEV_USD.Target_USD == 0)
//                        {
//                            rEV_USD.AT = 0;
//                        }
//                        else
//                        {
//                            rEV_USD.AT = Math.Round(rEV_USD.CY_USD - rEV_USD.Target_USD, 2);
//                        }
//                        //againest last year
//                        rEV_USD.ALY = Math.Round(rEV_USD.CY_USD - rEV_USD.LY_USD, 2);
//                        rEV_USD.Total = rEV_USD.CY_USD + "%";

//                        //audit trail
//                        rEV_USD.CreatedAt = DateTime.Now; ;
//                        rEV_USD.CreatedBy = loggedInUser.UserName;
//                        _context.Add(rEV_USD);
//                        await _context.SaveChangesAsync();
//                        TempData["SuccessMessage"] = "Values added succesfully.";
//                        return RedirectToAction(nameof(Index));
//                    }
//                    catch (Exception ex)
//                    {
//                        TempData["FailureAlertMessage"] = "Something wrong with the input. Please try again with the appropriate input";
//                    }
//                }
//            }
//            else
//            {
//                TempData["FailureAlertMessage"] = "Please try again.";
//            }
//            return View();
//        }

//        // GET: REV_USD/Edit/5
//        public async Task<IActionResult> Edit(long? id)
//        {
//            if (id == null || _context.REV_USD == null)
//            {
//                return NotFound();
//            }

//            var rEV_USD = await _context.REV_USD.FindAsync(id);
//            if (rEV_USD == null)
//            {
//                return NotFound();
//            }
//            return View(rEV_USD);
//        }

//        // POST: REV_USD/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(long id, REV_USD rEV_USD)
//        {
//            if (id != rEV_USD.Id)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//                DateTime inputDate = rEV_USD.Date;
//                int year = inputDate.Year;
//                int monthNum = inputDate.Month;
//                string monthName = inputDate.ToString("MMMM"); // Full month name
//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                rEV_USD.Date = new DateTime(year, monthNum, 1); // assigns 1 to the date part for consistency

//                var previous_date = _context.REV_USD.SingleOrDefault(x => x.Id == id);
//                // if the new assigned date is the same as the old record or if its not redundant 
//                if (rEV_USD.Date == previous_date.Date || !_context.OnlineSales.Where(x => x.Date == inputDate).Any())
//                {
//                    try
//                    {
//                        _context.Entry(previous_date).State = EntityState.Detached;

//                        rEV_USD.LY_USD = Convert.ToDouble(rEV_USD.LY_USD);
//                        rEV_USD.CY_USD = Convert.ToDouble(rEV_USD.CY_USD);
//                        rEV_USD.Target_USD = Convert.ToDouble(rEV_USD.Target_USD);

//                        // Date Related information
//                        rEV_USD.Month = monthAbbreviation;
//                        rEV_USD.Year = year;
//                        rEV_USD.MonthNum = monthNum;
//                        rEV_USD.MonthName = monthName;
//                        rEV_USD.Date = new DateTime(year, monthNum, 1);

//                        // againest target percentage
//                        if (rEV_USD.Target_USD == 0)
//                        {
//                            rEV_USD.AT = 0;
//                        }
//                        else
//                        {
//                            rEV_USD.AT = Math.Round(rEV_USD.CY_USD - rEV_USD.Target_USD, 2);
//                        } //againest last year
//                        rEV_USD.ALY = Math.Round(rEV_USD.CY_USD - rEV_USD.LY_USD, 2);

//                        rEV_USD.Total = rEV_USD.CY_USD + "%";

//                        //audit trail
//                        rEV_USD.UpdatedAt = DateTime.Now;
//                        rEV_USD.UpdatedBy = loggedInUser.UserName;
//                        rEV_USD.Status = Status.Active;

//                        _context.Update(rEV_USD);
//                        await _context.SaveChangesAsync();
//                        TempData["SuccessMessage"] = "Values edited succesfully.";
//                        return RedirectToAction(nameof(Index));
//                    }
//                    catch (DbUpdateConcurrencyException)
//                    {
//                        TempData["FailureAlertMessage"] = "Something wrong with the input. Please try again with the appropriate input";
//                    }
//                }
//                else
//                {
//                    TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
//                }
//            }
//            return View(rEV_USD);
//        }

//        // GET: REV_USD/Delete/5
//        public async Task<IActionResult> Delete(long? id)
//        {
//            if (id == null || _context.REV_USD == null)
//            {
//                return NotFound();
//            }

//            var rEV_USD = await _context.REV_USD
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (rEV_USD == null)
//            {
//                return NotFound();
//            }

//            return View(rEV_USD);
//        }

//        // POST: REV_USD/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(long id)
//        {
//            if (_context.REV_USD == null)
//            {
//                return Problem("Entity set 'DBContext.REV_USD'  is null.");
//            }
//            var rEV_USD = await _context.REV_USD.FindAsync(id);
//            if (rEV_USD != null)
//            {
//                //logged in user info
//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);

//                rEV_USD.UpdatedAt = DateTime.UtcNow;
//                rEV_USD.UpdatedBy = loggedInUser.UserName;
//                rEV_USD.Status = Status.Inactive;
//                await _context.SaveChangesAsync();
//                TempData["SuccessMessage"] = "Values deleted succesfully.";
//                return RedirectToAction(nameof(Index));
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool REV_USDExists(long id)
//        {
//            return (_context.REV_USD?.Any(e => e.Id == id)).GetValueOrDefault();
//        }
//    }
//}
