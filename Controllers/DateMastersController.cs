
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

namespace ADDPerformance.Controllers
{
    public class DateMastersController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DateMastersController(DBContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: DateMasters
        public async Task<IActionResult> Index()
        {
            // Only show Active dates for reporting clarity
            return _context.DateMaster != null ?
                        View(await _context.DateMaster.Where(x => x.Status == Status.Active).ToListAsync()) :
                        Problem("Entity set 'DBContext.DateMaster' is null.");
        }

        // GET: DateMasters/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateMaster dateMaster)
        {
            if (ModelState.IsValid)
            {
                // 1. Normalize Date to the 1st of the month
                dateMaster.Date = new DateTime(dateMaster.Date.Year, dateMaster.Date.Month, 1);

                // 2. Check for duplicates
                if (_context.DateMaster.Any(x => x.Date == dateMaster.Date && x.Status == Status.Active))
                {
                    ModelState.AddModelError("Date", "This month and year already exists in the master list.");
                    return View(dateMaster);
                }

                // 3. Auto-populate string fields based on the Date selected
                dateMaster.Month = dateMaster.Date.ToString("MMM").ToUpper();
                dateMaster.MonthName = dateMaster.Date.ToString("MMMM");
                dateMaster.MonthNum = dateMaster.Date.Month;
                dateMaster.Year = dateMaster.Date.Year;

                // 4. Audit Trail
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);

                dateMaster.Status = Status.Active;
                dateMaster.CreatedAt = DateTime.Now;
                dateMaster.CreatedBy = user?.UserName ?? "System";

                _context.Add(dateMaster);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Master Date added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(dateMaster);
        }

        // GET: DateMasters/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();
            var dateMaster = await _context.DateMaster.FindAsync(id);
            if (dateMaster == null) return NotFound();
            return View(dateMaster);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, DateMaster dateMaster)
        {
            if (id != dateMaster.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.DateMaster.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    // Normalize fields
                    dateMaster.Date = new DateTime(dateMaster.Date.Year, dateMaster.Date.Month, 1);
                    dateMaster.Month = dateMaster.Date.ToString("MMM").ToUpper();
                    dateMaster.MonthName = dateMaster.Date.ToString("MMMM");
                    dateMaster.MonthNum = dateMaster.Date.Month;
                    dateMaster.Year = dateMaster.Date.Year;

                    // Audit Trail
                    dateMaster.CreatedAt = existing.CreatedAt;
                    dateMaster.CreatedBy = existing.CreatedBy;
                    dateMaster.UpdatedAt = DateTime.Now;
                    dateMaster.UpdatedBy = User.Identity?.Name;
                    dateMaster.Status = Status.Active;

                    _context.Update(dateMaster);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DateMasterExists(dateMaster.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dateMaster);
        }

        // POST: DateMasters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var dateMaster = await _context.DateMaster.FindAsync(id);
            if (dateMaster != null)
            {
                // Soft Delete
                dateMaster.Status = Status.Inactive;
                dateMaster.UpdatedAt = DateTime.Now;
                dateMaster.UpdatedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Date deactivated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DateMasterExists(long id)
        {
            return (_context.DateMaster?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}