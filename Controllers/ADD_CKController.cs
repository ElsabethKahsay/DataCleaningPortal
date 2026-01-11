using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADDPerformance.Data;
using ADDPerformance.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ADDPerformance.Services;
using Microsoft.AspNetCore.Authorization;

namespace ADDPerformance.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AddCkController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAddCkService _addCkService;

        public AddCkController(DBContext context, UserManager<IdentityUser> userManager, IAddCkService addCkService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _addCkService = addCkService ?? throw new ArgumentNullException(nameof(addCkService));
        }

        /// <summary>
        /// Get all active ADD_CK records
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ADD_CK>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ADD_CK>>> GetAll()
        {
            var records = await _context.ADD_CK
                .Where(x => x.Status == Status.Active)
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return Ok(records);
        }

        /// <summary>
        /// Get single ADD_CK record by ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ADD_CK), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ADD_CK>> GetById(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            return record == null ? NotFound() : Ok(record);
        }

        /// <summary>
        /// Upload and process ADD_CK CSV file (upsert: update if exists, insert if new)
        /// Expected CSV format: Date (JAN-2024),CY,LY,Target
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Policy = "AdminOnly")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            var result = await _addCkService.ProcessAddCkCsvAsync(file, User, HttpContext.RequestAborted);
            if (result == null) return BadRequest(new { message = "Processing failed" });
            return Ok(result);
        }

        /// <summary>
        /// Soft delete an ADD_CK record
        /// </summary>
        [HttpDelete("{id:long}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(long id)
        {
            var record = await _context.ADD_CK.FindAsync(id);
            if (record == null)
                return NotFound();

            record.Status = Status.Inactive;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = await GetCurrentUsernameAsync() ?? "SYSTEM_API";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper methods
        private async Task<string?> GetCurrentUsernameAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.UserName;
        }
    }
}