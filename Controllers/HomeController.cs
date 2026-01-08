using ADDPerformance.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
       // POST: api/ADD_CK/upload
using Microsoft.AspNetCore.Identity;

using System.Security.Claims;
using ADDPerformance.Data;

namespace ADDPerformance.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(UserManager<IdentityUser> userManager, DBContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        [HttpPost("uploadFile")]
        [Consumes("multipart/form-data")] // Explicitly tells Swagger this is a form upload
        public async Task<IActionResult> Upload([FromForm] string fileTypeChoice, [FromForm] IFormFile file)
        {
            // 1.Validation
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }
            if (string.IsNullOrEmpty(fileTypeChoice))
                return BadRequest("Please provide a file type (e.g., 'Revenue', 'Destinations').");

            //  2.CSV Extension Check
            var fileExt = Path.GetExtension(file.FileName).ToLower();
            if (fileExt != ".csv")
                return BadRequest("Only .csv files are allowed.");

            //  3.File Size Check(e.g., 3MB limit)
            if (file.Length > 3 * 1024 * 1024)
                return BadRequest("File size exceeds the 5MB limit.");

            //  4.Identity Check
            var loggedInUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var loggedInUser = await _userManager.FindByIdAsync(loggedInUserId);

            //  5.Process based on File Type
            // You can now use 'fileType' to decide which logic to run
            switch (fileTypeChoice)
            {
                case 1: // ADD_CK
                    return await ProcessAddCkCsv(file, loggedInUser);

                case 2: // Revenue USD
                    return await ProcessRevenueCsv(file, loggedInUser);

                case 3: // Online Sales
                    return await ProcessOnlineSalesCsv(file, loggedInUser);

                case 4: // Destinations
                    return await ProcessDestinationsCsv(file, loggedInUser);

                case 5: // Corporate Sales
                    return await ProcessCorporateSalesCsv(file, loggedInUser);

                default:
                    return BadRequest("Invalid file type selection. Please choose a number between 1 and 5.");
            }
        }

        //   Helper method to keep the code clean
      
    }
}