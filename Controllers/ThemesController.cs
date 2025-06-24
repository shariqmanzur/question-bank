using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace QuestionBank.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class ThemesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThemesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Themes
        public async Task<IActionResult> Index()
        {
            var themes = await _context.Themes.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(themes);
        }

        // GET: Themes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var theme = await _context.Themes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (theme == null) return NotFound();

            return View(theme);
        }

        // GET: Themes/Create
        public IActionResult Create() => View();

        // POST: Themes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Theme theme)
        {
            if (!ModelState.IsValid) return View(theme);

            theme.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            theme.CreatedAt = DateTime.UtcNow;

            _context.Add(theme);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Theme created successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: Themes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var theme = await _context.Themes.FindAsync(id);
            if (theme == null) return NotFound();

            return View(theme);
        }

        // POST: Themes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Theme theme)
        {
            if (id != theme.Id) return NotFound();
            if (!ModelState.IsValid) return View(theme);

            try
            {
                var existingTheme = await _context.Themes.FindAsync(id);
                if (existingTheme == null) return NotFound();

                existingTheme.Name = theme.Name;
                existingTheme.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                existingTheme.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ThemeExists(theme.Id)) return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Theme updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: Themes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var theme = await _context.Themes.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (theme == null) return NotFound();

            return View(theme);
        }

        // POST: Themes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theme = await _context.Themes.FindAsync(id);
            if (theme == null) return NotFound();

            _context.Themes.Remove(theme);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Theme deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ThemeExists(int id)
        {
            return await _context.Themes.AnyAsync(e => e.Id == id);
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var theme = _context.Themes.Find(id);
            if (theme == null)
            {
                return Json(new { success = false, message = "Theme not found" });
            }

            theme.IsActive = !theme.IsActive;
            theme.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            theme.ModifiedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, isActive = theme.IsActive });
        }
    }
}
