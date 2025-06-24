using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using QuestionBank.Services;
using System.Linq;
using System.Security.Claims;

namespace QuestionBank.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class QuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;


        public QuestionsController(ApplicationDbContext context, INotificationService notificationService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Questions
        public async Task<IActionResult> Index()
        {
            var query = _context.Questions
                                .Include(q => q.Topic)
                                 .ThenInclude(q => q.Theme)
                                .Include(q => q.CompetencyLevel)
                                .OrderByDescending(q => q.CreatedAt)
                                .AsQueryable();

            // If the current user is not an Admin, filter to show only their own questions.
            if (!User.IsInRole("Admin"))
            {
                var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(q => q.CreatedBy == teacherId);
            }

            return View(await query.ToListAsync());
        }

        // GET: Questions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Topic)
                    .ThenInclude(t => t.Theme)
                .Include(q => q.CompetencyLevel)
                .Include(q => q.BCQAnswers)
                .Include(q => q.QuestionReviews)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            var reviewerIds = question.QuestionReviews
                .Select(r => r.CreatedBy)
                .Distinct()
                .ToList();

            var reviewerNames = await _context.Users
                .Where(u => reviewerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            ViewData["ReviewerNames"] = reviewerNames;

            return View(question);
        }

        // GET: Questions/Create
        // First step: select Theme, Topic, and Question Type
        public IActionResult Create()
        {
            // Pass all themes to the view.
            ViewBag.Themes = _context.Themes.OrderBy(t => t.Name).ToList();
            // Optionally, if you need competency levels in this view, you can add:
            //ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(cl => cl.Level).ToList();
            return View();
        }

        // POST: Questions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int themeId, int topicId, QuestionType questionType)
        {
            // Save selected TopicId and QuestionType to TempData for the next step.
            TempData["TopicId"] = topicId;
            TempData["QuestionType"] = questionType.ToString();
            TempData.Keep();
            // Redirect to the appropriate detailed create page.
            switch (questionType)
            {
                case QuestionType.BCQ:
                    return RedirectToAction(nameof(CreateBCQ));
                case QuestionType.SAQ:
                    return RedirectToAction(nameof(CreateSAQ));
                case QuestionType.Image:
                    return RedirectToAction(nameof(CreateImageQuestion));
                default:
                    return RedirectToAction(nameof(Index));
            }
        }

        // GET: Questions/CreateBCQ (Best Choice Question)
        public IActionResult CreateBCQ()
        {
            int topicId = Convert.ToInt32(TempData["TopicId"]);
            ViewBag.Topic = _context.Topics.Find(topicId);
            ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(c => c.Level).ToList();
            return View();
        }

        // POST: Questions/CreateBCQ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBCQ(Question question, List<string> AnswerTexts, int CorrectAnswerIndex)
        {
            if (ModelState.IsValid)
            {
                question.QuestionType = QuestionType.BCQ;
                question.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                question.CreatedAt = DateTime.UtcNow;

                _context.Add(question);
                await _context.SaveChangesAsync();

                for (int i = 0; i < AnswerTexts.Count; i++)
                {
                    var answer = new BCQAnswer
                    {
                        AnswerText = AnswerTexts[i],
                        IsCorrect = (i == CorrectAnswerIndex),
                        QuestionId = question.Id
                    };
                    _context.BCQAnswers.Add(answer);
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question created successfully";

                return RedirectToAction(nameof(Index));
            }
            return View(question);
        }

        // GET: Questions/CreateSAQ (Short Answer Question)
        public IActionResult CreateSAQ()
        {
            int topicId = Convert.ToInt32(TempData["TopicId"]);
            ViewBag.Topic = _context.Topics.Find(topicId);
            ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(c => c.Level).ToList();
            return View();
        }

        // POST: Questions/CreateSAQ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSAQ(Question question)
        {
            if (ModelState.IsValid)
            {
                question.QuestionType = QuestionType.SAQ;
                question.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                question.CreatedAt = DateTime.UtcNow;

                _context.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Question created successfully";

                return RedirectToAction(nameof(Index));
            }
            return View(question);
        }

        // GET: Questions/CreateImageQuestion (Image-Based Question)
        public IActionResult CreateImageQuestion()
        {
            int topicId = Convert.ToInt32(TempData["TopicId"]);
            ViewBag.Topic = _context.Topics.Find(topicId);
            ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(c => c.Level).ToList();
            return View();
        }

        // POST: Questions/CreateImageQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateImageQuestion(Question question, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                question.QuestionType = QuestionType.Image;
                question.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                question.CreatedAt = DateTime.UtcNow;

                _context.Add(question);
                await _context.SaveChangesAsync();

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = question.Id + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "QuestionImages", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    question.ImagePath = "/QuestionImages/" + fileName;
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Question created successfully";

                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // GET: Questions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Load question with Topic, its Theme, and BCQAnswers.
            var question = await _context.Questions
                .Include(q => q.Topic)
                    .ThenInclude(t => t.Theme)
                .Include(q => q.BCQAnswers) // Include BCQ answers
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            // Prepare dropdown lists.
            ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(cl => cl.Level).ToList();
            ViewBag.Themes = _context.Themes.OrderBy(t => t.Name).ToList();
            int currentThemeId = question.Topic?.ThemeId ?? 0;
            ViewBag.Topics = _context.Topics
                                .Where(t => t.ThemeId == currentThemeId)
                                .OrderBy(t => t.Name)
                                .ToList();

            return View(question);
        }


        // POST: Questions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int themeId, Question question, IFormFile? ImageFile, List<string>? AnswerTexts, int? CorrectAnswerIndex)
        {
            if (id != question.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Retrieve the original question to preserve its original values.
                var originalQuestion = await _context.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id);
                if (originalQuestion == null)
                    return NotFound();

                // Preserve critical fields.
                question.CreatedBy = originalQuestion.CreatedBy;
                question.CreatedAt = originalQuestion.CreatedAt;
                question.IsActive = originalQuestion.IsActive;

                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                question.ModifiedBy = currentUser;
                question.ModifiedAt = DateTime.UtcNow;

                // If the current user is a teacher and the original question was Accepted,
                // then automatically set the status to Pending and notify the admins.
                if (User.IsInRole("Teacher") && originalQuestion.Status == ReviewStatus.Accepted)
                {
                    question.Status = ReviewStatus.Pending;

                    // Retrieve all admin users.
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    foreach (var admin in adminUsers)
                    {
                        await _notificationService.SendNotificationAsync(
                            admin.Id,
                            $"Teacher '{User.FindFirstValue(ClaimTypes.Name)}' has updated the accepted question '{(question.QuestionText.Length > 30 ? question.QuestionText.Substring(0, 30) + "..." : question.QuestionText)}'.",
                            question.Id
                        );
                    }
                }
                else
                {
                    // Otherwise, preserve the existing status.
                    question.Status = originalQuestion.Status;
                }

                // Handle image upload if applicable.
                if (question.QuestionType == QuestionType.Image && ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = question.Id + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "QuestionImages", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    question.ImagePath = "/QuestionImages/" + fileName;
                }

                // Handle BCQ answers update if applicable.
                if (question.QuestionType == QuestionType.BCQ && AnswerTexts != null && CorrectAnswerIndex.HasValue)
                {
                    var existingAnswers = await _context.BCQAnswers.Where(a => a.QuestionId == question.Id).ToListAsync();
                    if (existingAnswers.Count == AnswerTexts.Count)
                    {
                        for (int i = 0; i < existingAnswers.Count; i++)
                        {
                            existingAnswers[i].AnswerText = AnswerTexts[i];
                            existingAnswers[i].IsCorrect = (i == CorrectAnswerIndex.Value);
                        }
                    }
                    else
                    {
                        _context.BCQAnswers.RemoveRange(existingAnswers);
                        foreach (var pair in AnswerTexts.Select((text, index) => new { text, index }))
                        {
                            var newAnswer = new BCQAnswer
                            {
                                AnswerText = pair.text,
                                IsCorrect = (pair.index == CorrectAnswerIndex.Value),
                                QuestionId = question.Id
                            };
                            _context.BCQAnswers.Add(newAnswer);
                        }
                    }
                }

                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.Id))
                        return NotFound();
                    else
                        throw;
                }

                TempData["SuccessMessage"] = "Question updated successfully";

                return RedirectToAction(nameof(Index));
            }

            // If ModelState is invalid, repopulate the dropdown lists.
            ViewBag.Themes = _context.Themes.OrderBy(t => t.Name).ToList();
            ViewBag.Topics = _context.Topics.Where(t => t.ThemeId == themeId).OrderBy(t => t.Name).ToList();
            ViewBag.CompetencyLevels = _context.CompetencyLevels.OrderBy(cl => cl.Level).ToList();
            return View(question);
        }

        // GET: Questions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var question = await _context.Questions
                .Include(q => q.Topic)
                    .ThenInclude(t => t.Theme)
                .Include(q => q.CompetencyLevel)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);

            if (question != null)
            {
                // If this is an image-based question and there is an image path, delete the file.
                if (question.QuestionType == QuestionType.Image && !string.IsNullOrEmpty(question.ImagePath))
                {
                    // The ImagePath is stored like "/QuestionImages/filename.jpg"
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", question.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Question deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }

        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var question = _context.Questions.Find(id);
            if (question == null)
            {
                return Json(new { success = false, message = "Question not found" });
            }

            question.IsActive = !question.IsActive;
            question.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            question.ModifiedAt = DateTime.Now;

            _context.SaveChanges();

            return Json(new { success = true, isActive = question.IsActive });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review()
        {
            var pendingQuestions = await _context.Questions
                .Where(q => q.Status == ReviewStatus.Pending)
                .Include(q => q.Topic)
                .ThenInclude(q => q.Theme)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(pendingQuestions);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            question.Status = ReviewStatus.Accepted;
            question.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            question.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify the teacher that their question has been accepted.
            await _notificationService.SendNotificationAsync(
                question.CreatedBy,
                $"Your question '{(question.QuestionText.Length > 30 ? question.QuestionText.Substring(0, 30) + "..." : question.QuestionText)}' has been accepted.",
                question.Id  // Pass the question id here
            );

            return RedirectToAction(nameof(Review));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuestion(int id, string comment)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            question.Status = ReviewStatus.Rejected;
            question.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin";
            question.ModifiedAt = DateTime.UtcNow;

            var reviewComment = new QuestionReview
            {
                QuestionId = id,
                Comment = comment,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Admin"
            };

            _context.QuestionReviews.Add(reviewComment);
            await _context.SaveChangesAsync();

            // Notify the teacher that their question has been rejected with a comment.
            await _notificationService.SendNotificationAsync(
                question.CreatedBy,
                $"Your question '{(question.QuestionText.Length > 30 ? question.QuestionText.Substring(0, 30) + "..." : question.QuestionText)}' has been rejected. Comment: {comment}",
                question.Id  // Pass the question id here
            );

            return RedirectToAction(nameof(Review));
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyRejectedQuestions()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var rejectedQuestions = await _context.Questions
                .Where(q => q.Status == ReviewStatus.Rejected && q.CreatedBy == teacherId)
                .Include(q => q.QuestionReviews)
                .Include(q => q.Topic).ThenInclude(q => q.Theme)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var reviewerIds = rejectedQuestions.SelectMany(q => q.QuestionReviews).Select(r => r.CreatedBy).Distinct();
            var reviewerNames = await _context.Users
                .Where(u => reviewerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName); // Or FullName

            ViewData["ReviewerNames"] = reviewerNames;

            return View(rejectedQuestions);
        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitQuestion(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Ensure only the owner can resubmit.
            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            if (question.CreatedBy != currentUser)
            {
                return Forbid();
            }

            // Only update status if not already pending.
            if (question.Status != ReviewStatus.Pending)
            {
                question.Status = ReviewStatus.Pending;
                question.ModifiedBy = currentUser;
                question.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Optionally, notify all Admins. (Assuming you use UserManager to get Admins as shown earlier.)
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await _notificationService.SendNotificationAsync(
                        admin.Id,
                        $"Teacher '{User.FindFirstValue(ClaimTypes.Name)}' has resubmitted the question: '{(question.QuestionText.Length > 30 ? question.QuestionText.Substring(0, 30) + "..." : question.QuestionText)}'.",
                        question.Id
                    );
                }
            }
            // If already pending, no duplicate notification is sent.
            return RedirectToAction("MyRejectedQuestions");
        }
    }
}
