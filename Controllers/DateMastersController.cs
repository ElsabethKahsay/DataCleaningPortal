//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using ADDPerformance.Data;
//using ADDPerformance.Models;

//namespace ADDPerformance.Controllers
//{
//    public class DateMastersController : Controller
//    {
//        private readonly DBContext _context;

//        public DateMastersController(DBContext context)
//        {
//            _context = context;
//        }

//        // GET: DateMasters
//        public async Task<IActionResult> Index()
//        {
//              return _context.DateMaster != null ? 
//                          View(await _context.DateMaster.ToListAsync()) :
//                          Problem("Entity set 'DBContext.DateMaster'  is null.");
//        }

//        // GET: DateMasters/Details/5
//        public async Task<IActionResult> Details(long? id)
//        {
//            if (id == null || _context.DateMaster == null)
//            {
//                return NotFound();
//            }

//            var dateMaster = await _context.DateMaster
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (dateMaster == null)
//            {
//                return NotFound();
//            }

//            return View(dateMaster);
//        }

//        // GET: DateMasters/Create
//        public IActionResult Create()
//        {
//            return View();
//        }

//        // POST: DateMasters/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("Id,Status,Date,Month,MonthName,MonthNum,Year")] DateMaster dateMaster)
//        {
//            if (ModelState.IsValid)
//            {
//                _context.Add(dateMaster);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            return View(dateMaster);
//        }

//        // GET: DateMasters/Edit/5
//        public async Task<IActionResult> Edit(long? id)
//        {
//            if (id == null || _context.DateMaster == null)
//            {
//                return NotFound();
//            }

//            var dateMaster = await _context.DateMaster.FindAsync(id);
//            if (dateMaster == null)
//            {
//                return NotFound();
//            }
//            return View(dateMaster);
//        }

//        // POST: DateMasters/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(long id, [Bind("Id,Status,Date,Month,MonthName,MonthNum,Year")] DateMaster dateMaster)
//        {
//            if (id != dateMaster.Id)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(dateMaster);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!DateMasterExists(dateMaster.Id))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            return View(dateMaster);
//        }

//        // GET: DateMasters/Delete/5
//        public async Task<IActionResult> Delete(long? id)
//        {
//            if (id == null || _context.DateMaster == null)
//            {
//                return NotFound();
//            }

//            var dateMaster = await _context.DateMaster
//                .FirstOrDefaultAsync(m => m.Id == id);
//            if (dateMaster == null)
//            {
//                return NotFound();
//            }

//            return View(dateMaster);
//        }

//        // POST: DateMasters/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(long id)
//        {
//            if (_context.DateMaster == null)
//            {
//                return Problem("Entity set 'DBContext.DateMaster'  is null.");
//            }
//            var dateMaster = await _context.DateMaster.FindAsync(id);
//            if (dateMaster != null)
//            {
//                _context.DateMaster.Remove(dateMaster);
//            }
            
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool DateMasterExists(long id)
//        {
//          return (_context.DateMaster?.Any(e => e.Id == id)).GetValueOrDefault();
//        }
//    }
//}
