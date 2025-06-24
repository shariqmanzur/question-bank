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
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Topics
        public async Task<IActionResult> Index()
        {
            var topics = await _context.Topics
                                       .AsNoTracking()
                                       .OrderByDescending(t => t.CreatedAt)
                                       .Include(t => t.Theme)
                                       .ToListAsync();
            return View(topics);
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics
                                      .AsNoTracking()
                                      .Include(t => t.Theme)
                                      .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null) return NotFound();

            return View(topic);
        }

        // GET: Topics/Create
        public async Task<IActionResult> Create()
        {
            await PopulateThemesDropdown();
            return View();
        }

        // POST: Topics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Topic topic)
        {
            if (!ModelState.IsValid)
            {
                await PopulateThemesDropdown();
                return View(topic);
            }

            topic.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            topic.CreatedAt = DateTime.UtcNow;

            _context.Add(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Topic created successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: Topics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null) return NotFound();

            await PopulateThemesDropdown();
            return View(topic);
        }

        // POST: Topics/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Topic topic)
        {
            if (id != topic.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateThemesDropdown();
                return View(topic);
            }

            try
            {
                var existingTopic = await _context.Topics.FindAsync(id);
                if (existingTopic == null) return NotFound();

                existingTopic.Name = topic.Name;
                existingTopic.ThemeId = topic.ThemeId;
                existingTopic.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                existingTopic.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TopicExists(topic.Id)) return NotFound();
                throw;
            }

            TempData["SuccessMessage"] = "Topic updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: Topics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var topic = await _context.Topics
                                      .AsNoTracking()
                                      .Include(t => t.Theme)
                                      .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null) return NotFound();

            return View(topic);
        }

        // POST: Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null) return NotFound();

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Topic deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetTopicsByTheme(int themeId)
        {
            var topics = await _context.Topics
                .Where(t => t.ThemeId == themeId && t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Json(topics);
        }

        private async Task<bool> TopicExists(int id)
        {
            return await _context.Topics.AnyAsync(e => e.Id == id);
        }

        private async Task PopulateThemesDropdown()
        {
            ViewBag.Themes = await _context.Themes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var topic = _context.Topics.Find(id);
            if (topic == null)
            {
                return Json(new { success = false, message = "Topic not found" });
            }

            topic.IsActive = !topic.IsActive;
            topic.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            topic.ModifiedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, isActive = topic.IsActive });
        }
    }
}
