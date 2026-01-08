using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using NuGet.Packaging.Signing;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("/CotporateSale")]
    public class CorporateSalesController : ControllerBase
    {
        public UserManager<IdentityUser> userManager { get; private set; }
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CorporateSalesController(UserManager<IdentityUser> _userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            this.userManager = _userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        Dictionary<string, (int Number, string FullName)> monthAbbreviationsToInfo = new Dictionary<string, (int, string)>
{
    {"JAN", (1, "January")}, {"FEB", (2, "February")}, {"MAR", (3, "March")},
    {"APR", (4, "April")}, {"MAY", (5, "May")}, {"JUN", (6, "June")},
    {"JUL", (7, "July")}, {"AUG", (8, "August")}, {"SEP", (9, "September")},
    {"OCT", (10, "October")}, {"NOV", (11, "November")}, {"DEC", (12, "December")}
};
        // GET: CorporateSales

       
[HttpPost("upload")]
[Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload()
        {
            // Retrieve uploaded files and form fields
            string url = string.Empty;
            var attachedFile = Request.Form.Files;
            var filtType = Request.Form["FileType"];
            // user identity for audit attribute
            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
            // Check if at least one file is attached
            if (attachedFile.Count > 0)
            {
                // Get information about the uploaded file
                var fileExt = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.LastOrDefault();
                var fileName = Path.GetExtension(attachedFile?.FirstOrDefault()?.FileName)?.Split('.')?.FirstOrDefault();
                var contentType = attachedFile?.FirstOrDefault()?.ContentType;
                var size = attachedFile?.FirstOrDefault()?.Length;
                var file = attachedFile[0];

                // Check if the uploaded file has a CSV extension
                if (fileExt.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        var UpdatedList = new List<CorporateSales>();
                        var isFirstLine = true; // Flag to identify the first line

                        var mainList = new List<CorporateSales>();
                        while (!reader.EndOfStream)
                        {
                            var data = reader.ReadLine();

                            if (data != string.Empty)
                            {
                                if (isFirstLine)
                                {
                                    isFirstLine = false;
                                    continue; // Skip processing the first line
                                }
                                else
                                {
                                    var values = data?.Split(',');
                                    // Extract and convert date from the CSV to string to apply the split() 
                                    var temp = Convert.ToString(values?[0]);

                                    // Extract and convert date from the CSV to datetime to check for redundance with existing data
                                    var tempDate = Convert.ToDateTime(values[0]);
                                    string[] tempparts = temp.Split('-');

                                    // Extract date parts for processing

                                    var Month = tempparts[0];  // Extract the three-letter month abbreviation
                                    var Year = int.Parse(tempparts[1]);

                                    if (monthAbbreviationsToInfo.TryGetValue(Month.ToUpper(), out var tempmonthInfo))
                                    {
                                        DateTime DateOnly = new DateTime(Year, tempmonthInfo.Number, 1);
                                        tempDate = DateOnly;
                                    }

                                    // Check if a record with the same date already exists in the database
                                    if (_context.CorporateSales.Where(x => x.Date == tempDate && x.CorpType == values[1]).Any())
                                    {
                                        // Update an existing record
                                        var existingRecord = _context.CorporateSales.FirstOrDefault(x => x.Date == tempDate);
                                        existingRecord.CY = Convert.ToDouble(values[2]);
                                        existingRecord.LY = Convert.ToDouble(values[3]);
                                        existingRecord.Target = Convert.ToDouble(values[4]);
                                        existingRecord.AT = Math.Round((existingRecord.CY - existingRecord.Target) / existingRecord.Target * 100, 2);
                                        existingRecord.ALY = Math.Round(existingRecord.CY - existingRecord.LY, 2);
                                        existingRecord.UpdatedAt = DateTime.Now; ;
                                        existingRecord.UpdatedBy = loggedInUser.UserName;
                                        UpdatedList.Add(existingRecord);
                                        _context.UpdateRange(UpdatedList);
                                    }
                                    else
                                    {
                                        // Create a new ADD_CK record
                                        CorporateSales line = new()
                                        {
                                            Date = Convert.ToDateTime(values[0]),
                                            CorpType = Convert.ToString(values[1]),
                                            CY = Convert.ToDouble(values[2]),
                                            LY = Convert.ToDouble(values[3]),
                                            Target = Convert.ToDouble(values[4]),
                                            Status = Status.Active
                                        };

                                        line.Month = Month;
                                        line.Year = Year;

                                        if (monthAbbreviationsToInfo.TryGetValue(line.Month.ToUpper(), out var monthInfo))
                                        {
                                            DateTime DateOnly = new DateTime(line.Year, monthInfo.Number, 1);
                                            line.Date = DateOnly.Date; // to make sure the date is always 1
                                            string[] dateParts = DateOnly.ToString().Split(' ');
                                            line.MonthNum = monthInfo.Number;
                                            line.MonthName = monthInfo.FullName;
                                        }
                                        if (line.Target == 0.0)
                                        {
                                            line.AT = 0.0;
                                        }
                                        else
                                        {
                                            // calculate againest target percentage rounded to 2 decimal points
                                            line.AT = Math.Round((line.CY - line.Target) / line.Target * 100, 2);
                                        }
                                        // againest  last year
                                        line.ALY = Math.Round(line.CY - line.LY, 2);

                                        //audit trail
                                        line.CreatedAt = DateTime.Now; ;
                                        line.CreatedBy = loggedInUser.UserName;
                                        mainList.Add(line);
                                        _context.AddRange(mainList);
                                    }
                                }
                            }
                        }
                        _context.AddRange(mainList);
                        await _context.SaveChangesAsync();
                       // TempData["SuccessMessage"] = "File successfully imported.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                 //   TempData["FailureAlertMessage"] = "Please choose a proper file format to import";
                }
            }
            else
            {
              //  TempData["FailureAlertMessage"] = "Please choose a  file to import";
            }

            return Ok();

        }

        // GET: CorporateSales/Details/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null || _context.CorporateSales == null)
            {
                return NotFound();
            }

            var corporateSales = await _context.CorporateSales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (corporateSales == null)
            {
                return NotFound();
            }

            return Ok(corporateSales);
        }

        // GET: CorporateSales/Create
       
        // POST: CorporateSales/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CorporateSales corporateSales)
        {
            var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
            if (ModelState.IsValid)
            {
                try
                {
                    // Extract and convert date from the CSV to string to apply the split() 
                    DateTime inputDate = corporateSales.Date;

                    int year = inputDate.Year;
                    int monthNum = inputDate.Month;
                    string monthName = inputDate.ToString("MMMM"); // Full month name
                    string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
                    DateTime newDate = new DateTime(year, monthNum, 1); // assigns 1 to the date part for consistency

                    if (_context.CorporateSales.Where(x => x.Date == newDate && x.CorpType == corporateSales.CorpType).Any())
                    {
                        //TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
                        return Ok(corporateSales);

                    }
                    CorporateSales line = new()
                    {
                        Date = newDate,
                        CorpType = Convert.ToString(corporateSales.CorpType),
                        CY = Convert.ToDouble(corporateSales.CY),
                        LY = Convert.ToDouble(corporateSales.LY),
                        Target = Convert.ToDouble(corporateSales.Target),
                    };

                    corporateSales.Month = monthAbbreviation;
                    corporateSales.Year = year;
                    corporateSales.MonthNum = monthNum;
                    corporateSales.MonthName = monthName;

                    if (corporateSales.Target == 0)
                    {
                        corporateSales.AT = 0;
                    }
                    else
                    {
                        corporateSales.AT = Math.Round(corporateSales.CY - corporateSales.Target / corporateSales.Target * 100, 2);
                    }
                    corporateSales.ALY = Math.Round(corporateSales.CY - corporateSales.LY, 2);
                    corporateSales.CreatedAt = DateTime.Now; ;
                    corporateSales.CreatedBy = loggedInUser.UserName;
                    corporateSales.Status = Status.Active;
                    _context.Add(corporateSales);
                    await _context.SaveChangesAsync();
                    //TempData["SuccessMessage"] = "Values edited succesfully.";
                    return Ok(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CorporateSalesExists(corporateSales.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                    //    TempData["FailureAlertMessage"] = "Something is wrong. Please try again.";
                        return Ok();
                    }
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: CorporateSales/Edit/5
            [HttpGet("{id}")]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null || _context.CorporateSales == null)
            {
                return NotFound();
            }

            var corporateSales = await _context.CorporateSales.FindAsync(id);
            if (corporateSales == null)
            {
                return NotFound();
            }
            return Ok(corporateSales);
        }

        // POST: CorporateSales/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, CorporateSales corporateSales)
        {
            if (id != corporateSales.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
                DateTime inputDate = corporateSales.Date;

                int year = inputDate.Year;
                int monthNum = inputDate.Month;
                string monthName = inputDate.ToString("MMMM"); // Full month name
                string monthAbbreviation = inputDate.ToString("MMM"); // Abbreviated and converted to uppercase
                DateTime newDate = new DateTime(year, monthNum, 1);
                var previous_date = _context.CorporateSales.SingleOrDefault(x => x.Id == id);
                // if the new assigned date is the same as the old record or if its not redundant 
                if (corporateSales.Date == previous_date.Date || !_context.CorporateSales.Where(x => x.Date == inputDate).Any())
                {
                    try
                    {
                        _context.Entry(previous_date).State = EntityState.Detached;
                        corporateSales.CY = Convert.ToDouble(corporateSales.CY);
                        corporateSales.LY = Convert.ToDouble(corporateSales.LY);
                        corporateSales.Target = Convert.ToDouble(corporateSales.Target);

                        //date related
                        corporateSales.Date = newDate;
                        corporateSales.Year = year;
                        corporateSales.Month = monthAbbreviation;
                        corporateSales.MonthNum = monthNum;
                        corporateSales.MonthName = monthName;

                        //handels if target is 0
                        if (corporateSales.Target == 0)
                        {
                            corporateSales.AT = 0;
                        }
                        else
                        {
                            corporateSales.AT = Math.Round(corporateSales.CY - corporateSales.Target / corporateSales.Target * 100, 2);
                        }
                        corporateSales.ALY = Math.Round(corporateSales.CY - corporateSales.LY, 2);

                        //audit trail
                        corporateSales.UpdatedAt = DateTime.Now;
                        corporateSales.UpdatedBy = loggedInUser.Email;
                        _context.Update(corporateSales);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {

                    }
                }
                else
                {
                    //TempData["FailureAlertMessage"] = "Date already exists. Please try again.";
                    return Ok(corporateSales);
                }
                return RedirectToAction(nameof(Index));
            }
            return Ok();
        }

        // GET: CorporateSales/Delete/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
          
            if (id == null || _context.CorporateSales == null)
            {
                return NotFound();
            }

            var corporateSales = await _context.CorporateSales
                .FirstOrDefaultAsync(m => m.Id == id);
            if (corporateSales == null)
            {
                return NotFound();
            }

            return Ok(corporateSales);
        }

        // POST: CorporateSales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            if (_context.CorporateSales == null)
            {
                return Problem("Entity set 'DBContext.CorporateSales'  is null.");
            }
            var corporateSales = await _context.CorporateSales.FindAsync(id);
            if (corporateSales != null)
            {
                var loggedInUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var loggedInUser = await userManager.FindByIdAsync(loggedInUserId);
                corporateSales.UpdatedAt = DateTime.UtcNow;
                corporateSales.UpdatedBy = loggedInUser.UserName;
                corporateSales.Status = Status.Inactive;

                _context.CorporateSales.Update(corporateSales);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CorporateSalesExists(long id)
        {
            return (_context.CorporateSales?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
