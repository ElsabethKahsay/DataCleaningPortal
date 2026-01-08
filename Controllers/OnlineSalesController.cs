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
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Components.Forms;
//using OnlineSales = ADDPerformance.Models.OnlineSales;



//namespace ADDPerformance.Controllers
//{
//    public class OnlineSalesController : Controller
//    {

//        public UserManager<IdentityUser> userManager { get; private set; }
//        private readonly DBContext _context;
//        private readonly IWebHostEnvironment _webHostEnvironment;

//        public OnlineSalesController(UserManager<IdentityUser> _userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
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
//            string fileName = "OnlineSales upload Template.csv";
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
//            // Retrieve uploaded files from the form
//            string url = string.Empty;
//            var attachedFile = Request.Form.Files;
//            var filtType = Request.Form["FileType"];

//            // logged in user information
//            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);

//            if (attachedFile.Count > 0)
//            {
//                // Get the file extension, name, content type, and size
//                var fileExt = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.LastOrDefault();
//                var fileName = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.FirstOrDefault();
//                var contentType = attachedFile.FirstOrDefault().ContentType;
//                var size = attachedFile.FirstOrDefault().Length;
//                var file = attachedFile[0];

//                // Check if the file is a CSV  file
//                if (fileExt.Equals("csv", StringComparison.OrdinalIgnoreCase))
//                {
//                    using (var reader = new StreamReader(file.OpenReadStream()))
//                    {
//                        var mainList = new List<OnlineSales>();
//                        var UpdatedList = new List<OnlineSales>();
//                        var isFirstLine = true; // Flag to identify the first line

//                        // Read the CSV file line by line
//                        while (!reader.EndOfStream)
//                        {
//                            var data = reader.ReadLine();
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
//                                    // Extract and convert date from the CSV to string to apply the split() 
//                                    var temp = Convert.ToString(values?[0]);

//                                    // Extract and convert date from the CSV to datetime to check for redundance with existing data
//                                    var tempDate = Convert.ToDateTime(values[0]);
//                                    string[] tempparts = temp.Split('-');

//                                    // Extract date parts for processing
//                                    var Month = tempparts[0];// Extract the three-letter month abbreviation
//                                    var Year = int.Parse(tempparts[1]);

//                                    if (monthAbbreviationsToInfo.TryGetValue(Month.ToUpper(), out var tempmonthInfo))
//                                    {
//                                        DateTime DateOnly = new DateTime(Year, tempmonthInfo.Number, 1);
//                                        tempDate = DateOnly;
//                                    }
//                                    var existingRecord = _context.OnlineSales.FirstOrDefault(x => x.Date == tempDate);

//                                    // Check if a record with the same date already exists in the database
//                                    if (existingRecord != null)
//                                    {
//                                        // Update an existing record
//                                        existingRecord.CYPercent = Convert.ToDouble(values[1]);
//                                        existingRecord.LYPercent = Convert.ToDouble(values[2]);
//                                        existingRecord.TargetPercent = Convert.ToDouble(values[3]);
//                                        existingRecord.AT = Math.Round((existingRecord.CYPercent - existingRecord.TargetPercent) / existingRecord.TargetPercent * 100, 2);
//                                        existingRecord.ALY = Math.Round(existingRecord.CYPercent - existingRecord.LYPercent, 2);
//                                        existingRecord.Total = existingRecord.CYPercent + "%";
//                                        existingRecord.Status = Status.Active;
//                                        existingRecord.UpdatedAt = System.DateTime.Now;
//                                        existingRecord.UpdatedBy = loggedInUser.Email;
//                                        UpdatedList.Add(existingRecord);
//                                        _context.UpdateRange(UpdatedList);
//                                    }
//                                    else
//                                    {
//                                        OnlineSales line = new()
//                                        {
//                                            Date = Convert.ToDateTime(values[0]),
//                                            CYPercent = Convert.ToDouble(values[1]),
//                                            LYPercent = Convert.ToDouble(values[2]),
//                                            TargetPercent = Convert.ToDouble(values[3]),
//                                        };

//                                        line.Month = temp.Substring(0, 3);
//                                        line.Year = Year;
//                                        if (monthAbbreviationsToInfo.TryGetValue(line.Month.ToUpper(), out var monthInfo))
//                                        {
//                                            DateTime DateOnly = new DateTime(line.Year, monthInfo.Number, 1);

//                                            string[] dateParts = DateOnly.ToString().Split(' ');
//                                            line.Date = DateOnly;
//                                            line.MonthNum = monthInfo.Number;
//                                            line.MonthName = monthInfo.FullName;
//                                        }

//                                        line.AT = Math.Round((line.CYPercent - line.TargetPercent) / line.TargetPercent * 100, 2);
//                                        line.Status = Status.Active;
//                                        line.ALY = Math.Round(line.CYPercent - line.LYPercent, 2);
//                                        line.Total = line.CYPercent + "%";

//                                        //Audit trail
//                                        line.CreatedAt = DateTime.Now;
//                                        line.CreatedBy = loggedInUser.UserName;
//                                        mainList.Add(line);
//                                        _context.AddRange(mainList);
//                                    }
//                                }
//                            }

//                        }

//                        try
//                        {

//                            await _context.SaveChangesAsync();
//                        }
//                        catch (Exception ex)
//                        {

//                        }
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
//        // GET: OnlineSales
//        public async Task<IActionResult> Index()
//        {
//            return _context.OnlineSales != null ?
//                        View(await _context.OnlineSales.Where(i => i.Status == Status.Active).ToListAsync()) :
//                        Problem("Entity set 'DBContext.OnlineSales'  is null.");
//        }

//        // GET: OnlineSales/Details/5
//        public async Task<IActionResult> Details(long? id)
//        {
//            if (id == null || _context.OnlineSales == null)
//            {
//                return NotFound();
//            }

//            var onlineSales = await _context.OnlineSales
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (onlineSales == null)
//            {
//                return NotFound();
//            }

//            return View(onlineSales);
//        }

//        // GET: OnlineSales/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: OnlineSales/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(OnlineSales onlineSales)
//        {
//            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//            if (ModelState.IsValid)
//            {
//                DateTime inputDate = onlineSales.Date;

//                int year = inputDate.Year;
//                int monthNum = inputDate.Month;

//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                DateTime newDate = new DateTime(year, monthNum, 1); // assigns 1 to the date part for consistency

//                if (_context.OnlineSales.Where(x => x.Date == newDate).Any())
//                {
//                    TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
//                }
//                try
//                {
//                    onlineSales.LYPercent = Convert.ToDouble(onlineSales.LYPercent);
//                    var temp = onlineSales.Date.ToString();
//                    onlineSales.Date = newDate;
//                    onlineSales.CYPercent = Convert.ToDouble(onlineSales.CYPercent);
//                    onlineSales.CYPercent = Convert.ToDouble(onlineSales.CYPercent);
//                    onlineSales.Month = monthAbbreviation;
//                    onlineSales.Year = year;
//                    onlineSales.MonthNum = monthNum;
//                    onlineSales.MonthName = inputDate.ToString("MMMM");   // Full month name

//                    // calculates againest target
//                    onlineSales.AT = Math.Round(onlineSales.CYPercent - onlineSales.TargetPercent, 2);

//                    // calculates againest last year
//                    onlineSales.ALY = Math.Round(onlineSales.CYPercent - onlineSales.LYPercent, 2);
//                    onlineSales.Total = onlineSales.CYPercent + "%";

//                    // Audit trail
//                    onlineSales.CreatedAt = DateTime.Now; ;
//                    onlineSales.CreatedBy = loggedInUser.UserName;
//                    _context.Add(onlineSales);
//                    await _context.SaveChangesAsync();
//                    TempData["SuccessMessage"] = "Values added succesfully.";
//                    return RedirectToAction(nameof(Index));
//                }
//                catch (Exception ex)
//                {
//                    TempData["FailureAlertMessage"] = "Something wrong with the input. Please try again with the appropriate input";
//                }
//            }
//            else
//            {
//                TempData["FailureAlertMessage"] = "Please try again.";
//            }
//            return View();
//        }

//        // GET: OnlineSales/Edit/5
//        public async Task<IActionResult> Edit(long? id)
//        {
//            if (id == null || _context.OnlineSales == null)
//            {
//                return NotFound();
//            }

//            var onlineSales = await _context.OnlineSales.FindAsync(id);
//            if (onlineSales == null)
//            {
//                return NotFound();
//            }
//            return View(onlineSales);
//        }

//        // POST: OnlineSales/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(long id, OnlineSales onlineSales)
//        {
//            if (id != onlineSales.Id)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
//                DateTime inputDate = onlineSales.Date;

//                int year = inputDate.Year;
//                int monthNum = inputDate.Month;

//                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
//                DateTime newDate = new DateTime(year, monthNum, 1); // assigns 1 to the date part for consistency

//                var previous_date = _context.OnlineSales.Where(x => x.Id == id).FirstOrDefault();
//                if (onlineSales.Date == previous_date.Date || !_context.OnlineSales.Where(x => x.Date == inputDate).Any())
//                {
//                    try
//                    {
//                        _context.Entry(previous_date).State = EntityState.Detached;
//                        onlineSales.LYPercent = Convert.ToDouble(onlineSales.LYPercent);
//                        onlineSales.CYPercent = Convert.ToDouble(onlineSales.CYPercent);

//                        //date related                        
//                        onlineSales.Month = inputDate.ToString("MMM");
//                        onlineSales.Year = inputDate.Year;
//                        onlineSales.Status = Status.Active;
//                        onlineSales.MonthNum = inputDate.Month;
//                        onlineSales.MonthName = inputDate.ToString("MMMM");   // Full month name
//                        onlineSales.Date = new DateTime(onlineSales.Year, onlineSales.MonthNum, 1);

//                        onlineSales.AT = Math.Round(onlineSales.CYPercent - onlineSales.TargetPercent, 2);
//                        onlineSales.ALY = Math.Round(onlineSales.CYPercent - onlineSales.LYPercent, 2);
//                        onlineSales.Total = onlineSales.CYPercent + "%";

//                        //Audit trail
//                        onlineSales.UpdatedAt = DateTime.UtcNow;
//                        onlineSales.UpdatedBy = loggedInUser.UserName;
//                        _context.Update(onlineSales);
//                        await _context.SaveChangesAsync();
//                        TempData["SuccessMessage"] = "Values added succesfully.";
//                        return RedirectToAction(nameof(Index));
//                    }
//                    catch (Exception ex)
//                    {
//                        TempData["FailureAlertMessage"] = "Something wrong with the input. Please try again with the appropriate input";
//                    }

//                }
//                else
//                {
//                    TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
//                }

//            }
//            return View(onlineSales);
//        }

//        // GET: OnlineSales/Delete/5
//        public async Task<IActionResult> Delete(long? id)
//        {
//            if (id == null || _context.OnlineSales == null)
//            {
//                return NotFound();
//            }

//            var onlineSales = await _context.OnlineSales
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (onlineSales == null)
//            {
//                return NotFound();
//            }

//            return View(onlineSales);
//        }

//        // POST: OnlineSales/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(long id)
//        {
//            if (_context.OnlineSales == null)
//            {
//                return Problem("Entity set 'DBContext.OnlineSales'  is null.");
//            }
//            var onlineSales = await _context.OnlineSales.FindAsync(id);
//            if (onlineSales != null)
//            {
//                //logged in user info
//                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);

//                onlineSales.UpdatedAt = DateTime.UtcNow;
//                onlineSales.UpdatedBy = loggedInUser.UserName;
//                onlineSales.Status = Status.Inactive;
//                await _context.SaveChangesAsync();
//                TempData["SuccessMessage"] = "Values deleted succesfully.";
//                return RedirectToAction(nameof(Index));
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool OnlineSalesExists(long id)
//        {
//            return (_context.OnlineSales?.Any(e => e.Id == id)).GetValueOrDefault();
//        }
//    }
//}
