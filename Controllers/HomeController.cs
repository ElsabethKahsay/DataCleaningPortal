using ADDPerformance.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
       // POST: api/ADD_CK/upload
using Microsoft.AspNetCore.Identity;
using ADD_CKController = ADDPerformance.Controllers.ADD_CKController;
using Microsoft.AspNetCore.Hosting;
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
[Consumes("multipart/form-data")]
public async Task<IActionResult> Upload([FromForm] string fileTypeChoice, [FromForm] IFormFile file)
{
    if (file == null || file.Length == 0) return BadRequest("No file was uploaded.");
    if (string.IsNullOrEmpty(fileTypeChoice)) return BadRequest("Please provide a file type (e.g., '1').");

    var fileExt = Path.GetExtension(file.FileName).ToLower();
    if (fileExt != ".csv") return BadRequest("Only .csv files are allowed.");
    if (file.Length > 3 * 1024 * 1024) return BadRequest("File size exceeds the 3MB limit.");

    var loggedInUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var loggedInUser = await _userManager.FindByIdAsync(loggedInUserId);

    if (!int.TryParse(fileTypeChoice, out var fileTypeInt))
        return BadRequest("Invalid file type. Provide a number (e.g., 1).");

    switch (fileTypeInt)
    {
        case 1: // ADD_CK
            var addCkController = new ADD_CKController(_userManager, _context, _webHostEnvironment);
            return await addCkController.ProcessAddCkCsv(file, fileTypeChoice, loggedInUser);

        case 2: // Revenue USD
        case 3: // Online Sales
        case 4: // Destinations
        case 5: // Corporate Sales
            return BadRequest("Processing for selected file type is not implemented via this endpoint.");

        default:
            return BadRequest("Invalid file type selection. Please choose a number between 1 and 5.");
    }
}
    }
}