using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuestionBank.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class CompetencyLevelsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompetencyLevelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CompetencyLevels
        public async Task<IActionResult> Index()
        {
            var competencyLevels = await _context.CompetencyLevels
                                                 .AsNoTracking()
                                                 .OrderByDescending(c => c.CreatedAt)
                                                 .ToListAsync();
            return View(competencyLevels);
        }

        // GET: CompetencyLevels/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var competencyLevel = await _context.CompetencyLevels
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(m => m.Id == id);
            if (competencyLevel == null) return NotFound();

            return View(competencyLevel);
        }

        // GET: CompetencyLevels/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CompetencyLevels/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompetencyLevel competencyLevel)
        {
            if (!ModelState.IsValid)
            {
                return View(competencyLevel);
            }

            competencyLevel.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            competencyLevel.CreatedAt = DateTime.UtcNow;

            _context.Add(competencyLevel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Competency Level created successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: CompetencyLevels/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var competencyLevel = await _context.CompetencyLevels.FindAsync(id);
            if (competencyLevel == null) return NotFound();

            return View(competencyLevel);
        }

        // POST: CompetencyLevels/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompetencyLevel competencyLevel)
        {
            if (id != competencyLevel.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(competencyLevel);
            }

            try
            {
                var existingCompetencyLevel = await _context.CompetencyLevels.FindAsync(id);
                if (existingCompetencyLevel == null) return NotFound();

                existingCompetencyLevel.Level = competencyLevel.Level;
                existingCompetencyLevel.Description = competencyLevel.Description;
                existingCompetencyLevel.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                existingCompetencyLevel.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CompetencyLevelExists(competencyLevel.Id)) return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Competency Level updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: CompetencyLevels/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var competencyLevel = await _context.CompetencyLevels
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(m => m.Id == id);
            if (competencyLevel == null) return NotFound();

            return View(competencyLevel);
        }

        // POST: CompetencyLevels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competencyLevel = await _context.CompetencyLevels.FindAsync(id);
            if (competencyLevel == null) return NotFound();

            _context.CompetencyLevels.Remove(competencyLevel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Competency Level deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CompetencyLevelExists(int id)
        {
            return await _context.CompetencyLevels.AnyAsync(e => e.Id == id);
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var competencyLevel = _context.CompetencyLevels.Find(id);
            if (competencyLevel == null)
            {
                return Json(new { success = false, message = "Competency Level not found" });
            }

            competencyLevel.IsActive = !competencyLevel.IsActive;
            competencyLevel.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            competencyLevel.ModifiedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, isActive = competencyLevel.IsActive });
        }

    }
}
