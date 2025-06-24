using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using QuestionBank.ViewModels;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuestionBank.Utils;
using System.Text;
using Microsoft.AspNetCore.Identity;    // <- for MathConverter

namespace QuestionBank.Controllers
{
    public static class RegexExtensions
    {
        public static async Task<string> ReplaceAsync(
            this Regex regex,
            string input,
            Func<Match, Task<string>> replacementFn)
        {
            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in regex.Matches(input))
            {
                sb.Append(input, lastIndex, match.Index - lastIndex);
                sb.Append(await replacementFn(match));
                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }
    }

    [Authorize(Roles = "Admin")]
    public class PaperController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PaperController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Paper
        public async Task<IActionResult> Index()
        {
            // Show any TempData alerts
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            var list = await _context.Papers
                .Where(p => !p.IsArchived)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaperSummaryViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    UniversityName = p.UniversityName,
                    CampusName = p.CampusName,
                    Department = p.Department,
                    CourseName = p.CourseName,
                    CourseCode = p.CourseCode,
                    Semester = p.Semester,
                    ExamDate = p.ExamDate,
                    Duration = p.Duration,
                    TotalMarks = p.TotalMarks,
                    Instructions = p.Instructions,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return View(list);
        }

        // POST: /Paper/Archive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int paperId, string adminPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Not authorized" });

            if (!await _userManager.CheckPasswordAsync(user, adminPassword))
                return Json(new { success = false, message = "Invalid password" });

            var paper = await _context.Papers.FindAsync(paperId);
            if (paper == null)
                return Json(new { success = false, message = "Paper not found" });

            paper.IsArchived = true;
            paper.ModifiedBy = user.Id;
            paper.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Paper archived successfully" });
        }

        // GET: Paper/Create
        public async Task<IActionResult> Create()
        {
            var model = new PaperMakingViewModel
            {
                // UniversityName already defaults in VM,
                // FilterApplied = false by default,
                AvailableQuestions = new()
            };
            await PopulateDropdownsAsync(model);
            await PopulateSavedFiltersAsync(model);
            return View(model);
        }

        // POST: Paper/Filter
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Filter(PaperMakingViewModel model)
        {
            // 1) Only auto-load a saved filter when the user hasn't typed a NewFilterTitle
            if (model.PaperFilterId.HasValue
                && string.IsNullOrWhiteSpace(model.NewFilterTitle))
            {
                var saved = await _context.PaperFilters
                                         .FirstOrDefaultAsync(f => f.Id == model.PaperFilterId);
                if (saved != null)
                {
                    model.SelectedThemeId = saved.ThemeId;
                    model.SelectedTopicId = saved.TopicId;
                    model.SelectedCompetencyLevelId = saved.CompetencyLevelId;
                    model.SelectedTeacherId = saved.TeacherId;
                    model.StartDate = saved.StartDate;
                    model.EndDate = saved.EndDate;
                    model.SelectedQuestionTypesWithCount
                         = JsonSerializer
                           .Deserialize<Dictionary<string, int>>(saved.TypeCountJson)
                         ?? new Dictionary<string, int>();
                }
            }

            // 2) Always re-populate the dropdown lists & saved-filters list
            await PopulateDropdownsAsync(model);
            await PopulateSavedFiltersAsync(model);

            // 3) Build your question query
            var query = _context.Questions
                                .Where(q => q.IsActive && q.Status == ReviewStatus.Accepted)
                                .Include(q => q.Topic).ThenInclude(t => t.Theme)
                                .Include(q => q.CompetencyLevel)
                                .AsQueryable();

            if (model.SelectedThemeId.HasValue)
                query = query.Where(q => q.Topic.ThemeId == model.SelectedThemeId.Value);

            if (model.SelectedTopicId.HasValue)
                query = query.Where(q => q.TopicId == model.SelectedTopicId.Value);

            if (model.SelectedCompetencyLevelId.HasValue)
                query = query.Where(q => q.CompetencyLevelId == model.SelectedCompetencyLevelId.Value);

            if (!string.IsNullOrEmpty(model.SelectedTeacherId))
                query = query.Where(q => q.CreatedBy == model.SelectedTeacherId);

            if (model.StartDate.HasValue)
                query = query.Where(q => q.CreatedAt >= model.StartDate.Value);

            if (model.EndDate.HasValue)
                query = query.Where(q => q.CreatedAt <= model.EndDate.Value);

            // 4) Pick random questions by type/count
            var filteredQuestions = new List<Question>();
            foreach (var kv in model.SelectedQuestionTypesWithCount)
            {
                if (Enum.TryParse<QuestionType>(kv.Key, out var qt))
                {
                    var pick = await query
                       .Where(q => q.QuestionType == qt)
                       .OrderBy(_ => Guid.NewGuid())
                       .Take(kv.Value)
                       .ToListAsync();
                    filteredQuestions.AddRange(pick);
                }
            }
            model.AvailableQuestions = filteredQuestions;
            model.FilterApplied = true;

            // 5) Only *save* a brand-new filter if they gave a NewFilterTitle
            if (!string.IsNullOrWhiteSpace(model.NewFilterTitle))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var exists = await _context.PaperFilters
                                  .AnyAsync(f => f.Title == model.NewFilterTitle
                                              && f.CreatedBy == userId);
                if (!exists)
                {
                    var nf = new PaperFilter
                    {
                        Title = model.NewFilterTitle,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        ThemeId = model.SelectedThemeId,
                        TopicId = model.SelectedTopicId,
                        CompetencyLevelId = model.SelectedCompetencyLevelId,
                        TeacherId = model.SelectedTeacherId,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        TypeCountJson = JsonSerializer.Serialize(model.SelectedQuestionTypesWithCount)
                    };
                    _context.PaperFilters.Add(nf);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Filter saved successfully";

                    // refresh the in-memory list
                    await PopulateSavedFiltersAsync(model);
                }
                else
                {
                    TempData["Error"] = "A filter with this title already exists";
                }
            }

            // clear ModelState so dropdowns re-bind
            ModelState.Clear();

            return View("Create", model);
        }

        // POST: Paper/Paper  ← this now does validation + persistence
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Paper(PaperMakingViewModel model)
        {
            // Strip out filter props (Option A, but now VM has [ValidateNever],
            // so these lines are optional—ModelState won’t ever contain them)

            // If Duration is in hh:mm or hh:mm:ss format, convert it into fractional hours
            if (!string.IsNullOrWhiteSpace(model.Duration))
            {
                var parts = model.Duration.Split(':');
                if (parts.Length == 2 || parts.Length == 3)
                {
                    if (double.TryParse(parts[0], out var hours) &&
                        double.TryParse(parts[1], out var minutes))
                    {
                        model.DurationInHours = hours + (minutes / 60);
                    }
                    else
                    {
                        ModelState.AddModelError(
                            "Duration",
                            "Invalid duration format. Please use HH:mm or HH:mm:ss format"
                        );
                    }
                }
                else
                {
                    ModelState.AddModelError(
                        "Duration",
                        "Invalid duration format. Please use HH:mm or HH:mm:ss format"
                    );
                }
            }
            else
            {
                ModelState.AddModelError("Duration", "Duration is required");
            }

            var filterKeys = new[]
            {
                nameof(model.SelectedThemeId),
                nameof(model.SelectedTopicId),
                nameof(model.SelectedCompetencyLevelId),
                nameof(model.SelectedTeacherId),
                nameof(model.SelectedQuestionType),
                nameof(model.StartDate),
                nameof(model.EndDate),
                nameof(model.FilterApplied)
            };
            foreach (var k in filterKeys) ModelState.Remove(k);

            // 1) Validate only paper metadata
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                await PopulateSavedFiltersAsync(model);
                if (model.SelectedQuestionIds?.Any() == true)
                {
                    model.AvailableQuestions = await _context.Questions
                        .Where(q => model.SelectedQuestionIds.Contains(q.Id))
                        .Include(q => q.Topic).ThenInclude(t => t.Theme)
                        .Include(q => q.CompetencyLevel)
                        .ToListAsync();
                }
                // PRESERVE the filter UI
                model.FilterApplied = true;
                return View("Create", model);
            }

            // 2) Must select at least one question
            if (model.SelectedQuestionIds == null || !model.SelectedQuestionIds.Any())
            {
                ModelState.AddModelError("", "Please select at least one question");
                await PopulateDropdownsAsync(model);
                await PopulateSavedFiltersAsync(model);
                // Make sure the filter UI still shows “filtered” state
                model.FilterApplied = true;
                return View("Create", model);
            }

            // 3) Persist the paper with correct duration
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var paper = new Paper
            {
                Title = model.PaperTitle,
                UniversityName = model.UniversityName,
                CampusName = model.CampusName,
                Department = model.Department,
                CourseName = model.CourseName,
                CourseCode = model.CourseCode,
                Semester = model.Semester,
                ExamDate = model.ExamDate,
                Duration = model.DurationInHours.HasValue ? TimeSpan.FromHours(model.DurationInHours.Value) : TimeSpan.Zero, // Handle nullable duration
                TotalMarks = model.TotalMarks,
                Instructions = model.Instructions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            foreach (var qid in model.SelectedQuestionIds)
                paper.PaperQuestions.Add(new PaperQuestion { QuestionId = qid });

            _context.Papers.Add(paper);
            await _context.SaveChangesAsync();

            // 4) Load questions for print view
            var savedQuestions = await _context.Questions
                .Where(q => model.SelectedQuestionIds.Contains(q.Id))
                .Include(q => q.Topic).ThenInclude(t => t.Theme)
                .Include(q => q.CompetencyLevel)
                .Include(q => q.BCQAnswers)
                .ToListAsync();

            // Pass both the questions and the paper id
            ViewBag.PaperId = paper.Id;
            return View("Paper", savedQuestions);

            //return View("Paper", savedQuestions);
        }

        /// <summary>
        /// Returns all active topics for a given theme as JSON.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetTopics(int themeId)
        {
            var topics = await _context.Topics
                .Where(t => t.IsActive && t.ThemeId == themeId)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Json(topics);
        }

        // Helper method to populate dropdown lists
        private async Task PopulateDropdownsAsync(PaperMakingViewModel model)
        {
            model.Themes = await _context.Themes
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // ← New: populate topics if a theme is already chosen
            if (model.SelectedThemeId.HasValue)
            {
                model.AvailableTopics = await _context.Topics
                    .Where(t => t.IsActive && t.ThemeId == model.SelectedThemeId.Value)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }

            model.CompetencyLevels = await _context.CompetencyLevels
                .Where(c => c.IsActive)
                .OrderBy(c => c.Level)
                .ToListAsync();

            model.Teachers = await (from u in _context.Users
                                    join q in _context.Questions
                                      on u.Id equals q.CreatedBy
                                    where q.IsActive && q.Status == ReviewStatus.Accepted
                                    select new { u.Id, u.UserName })
                                   .Distinct()
                                   .OrderBy(x => x.UserName)
                                   .Select(x => new SelectListItem
                                   {
                                       Value = x.Id,
                                       Text = x.UserName
                                   })
                                   .ToListAsync();

            model.QuestionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = QuestionType.BCQ.ToString(),   Text = "BCQ" },
                new SelectListItem { Value = QuestionType.SAQ.ToString(),   Text = "SAQ" },
                new SelectListItem { Value = QuestionType.Image.ToString(), Text = "Image‑Based" }
            };
        }

        // Load Filters into ViewModel:
        private async Task PopulateSavedFiltersAsync(PaperMakingViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.PaperFilters = await _context.PaperFilters
                .Where(f => f.CreatedBy == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Title
                }).ToListAsync();
        }

        // DELETE SAVED FILTER
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFilter(int? paperFilterId)
        {
            if (paperFilterId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var filter = await _context.PaperFilters
                                  .FirstOrDefaultAsync(f => f.Id == paperFilterId.Value
                                                         && f.CreatedBy == userId);
                if (filter != null)
                {
                    _context.PaperFilters.Remove(filter);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Filter {filter.Title} deleted";
                }
                else
                {
                    TempData["Error"] = "Filter not found or you don’t have permission to delete it";
                }
            }
            return RedirectToAction(nameof(Create));
        }

        // EDIT SAVED FILTER
        [HttpGet]
        public async Task<JsonResult> GetFilterDetails(int paperFilterId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var f = await _context.PaperFilters
                        .FirstOrDefaultAsync(x => x.Id == paperFilterId && x.CreatedBy == userId);

            if (f == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                themeId = f.ThemeId,
                topicId = f.TopicId,
                levelId = f.CompetencyLevelId,
                teacherId = f.TeacherId,
                startDate = f.StartDate?.ToString("yyyy-MM-dd"),
                endDate = f.EndDate?.ToString("yyyy-MM-dd"),
                counts = JsonSerializer.Deserialize<Dictionary<string, int>>(f.TypeCountJson),
                title = f.Title
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetQuestionTypeCounts(int filterId)
        {
            var filter = await _context.PaperFilters
                               .AsNoTracking()
                               .FirstOrDefaultAsync(f => f.Id == filterId);

            if (filter == null)
                return Json(new { success = false });

            // Deserialize the stored JSON into a Dictionary<string,int>
            var counts = JsonSerializer
                .Deserialize<Dictionary<string, int>>(filter.TypeCountJson)
                ?? new Dictionary<string, int>();

            return Json(new
            {
                success = true,
                counts
            });
        }

        // -------------- NEW: Print as PDF ----------------
        [HttpGet]
        public async Task<IActionResult> Print(int paperId)
        {
            var paper = await _context.Papers
                .Include(p => p.PaperQuestions)
                  .ThenInclude(pq => pq.Question)
                    .ThenInclude(q => q.Topic).ThenInclude(t => t.Theme)
                .Include(p => p.PaperQuestions)
                  .ThenInclude(pq => pq.Question)
                    .ThenInclude(q => q.CompetencyLevel)
                .Include(p => p.PaperQuestions)
                  .ThenInclude(pq => pq.Question)
                    .ThenInclude(q => q.BCQAnswers)
                .FirstOrDefaultAsync(p => p.Id == paperId);

            if (paper == null) return NotFound();

            var questions = paper.PaperQuestions.Select(pq => pq.Question).ToList();

            // Pre-render all math content to SVG images
            foreach (var q in questions)
            {
                q.QuestionText = await ConvertMathToImages(q.QuestionText);

                if (q.QuestionType == QuestionType.BCQ && q.BCQAnswers != null)
                {
                    foreach (var ans in q.BCQAnswers)
                    {
                        ans.AnswerText = await ConvertMathToImages(ans.AnswerText);
                    }
                }
            }

            // 3) Local helper to humanize the TimeSpan
            string FormatDuration(TimeSpan ts)
            {
                var parts = new List<string>();
                if (ts.Hours > 0)
                    parts.Add($"{ts.Hours} hour{(ts.Hours == 1 ? "" : "s")}");
                if (ts.Minutes > 0)
                    parts.Add($"{ts.Minutes} minute{(ts.Minutes == 1 ? "" : "s")}");
                if (parts.Count == 0)
                    parts.Add("0 minutes");
                return string.Join(" ", parts);
            }

            // Build view model
            var vm = new PaperMakingViewModel
            {
                PaperTitle = paper.Title,
                UniversityName = paper.UniversityName,
                CampusName = paper.CampusName,
                Department = paper.Department,
                CourseName = paper.CourseName,
                CourseCode = paper.CourseCode,
                Semester = paper.Semester,
                ExamDate = paper.ExamDate,
                Duration = FormatDuration(paper.Duration),
                TotalMarks = paper.TotalMarks,
                Instructions = paper.Instructions,
                AvailableQuestions = questions
            };

            // Simplified PDF settings
            return new ViewAsPdf("PaperPdf", vm)
            {
                FileName = $"{paper.Title}.pdf",
                PageSize = Size.A4,
                PageOrientation = Orientation.Portrait,
                PageMargins = new Margins(20, 20, 20, 20),
                CustomSwitches = "--print-media-type --enable-local-file-access " +
                                 "--footer-center \"Page [page] of [toPage]\" " +
                                 "--footer-font-size 8 --footer-font-name \"Times New Roman\""
            };
        }

        private async Task<string> ConvertMathToImages(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            // 1) Display math: $$…$$ or \[…\]
            content = await ProcessPattern(
                content,
                @"\$\$(.+?)\$\$|\\\[(.+?)\\\]",
                async match =>
                {
                    var tex = match.Groups[1].Success
                              ? match.Groups[1].Value
                              : match.Groups[2].Value;
                    var uri = await MathConverter.ToSvgDataUriAsync(tex);
                    return string.IsNullOrEmpty(uri)
                        ? match.Value
                        : $"<img src='{uri}' class='math-img math-display' />";
                }
            );

            // 2) Inline math: single‐dollar $…$ (but not $$…$$)
            content = await ProcessPattern(
                content,
                @"(?<!\$)\$(.+?)(?<!\$)\$",
                async match =>
                {
                    var tex = match.Groups[1].Value;
                    var uri = await MathConverter.ToSvgDataUriAsync(tex);
                    return string.IsNullOrEmpty(uri)
                        ? match.Value
                        : $"<img src='{uri}' class='math-img math-inline' />";
                }
            );

            // 3) Inline math: \(…\)
            content = await ProcessPattern(
                content,
                @"\\\((.+?)\\\)",
                async match =>
                {
                    var tex = match.Groups[1].Value;
                    var uri = await MathConverter.ToSvgDataUriAsync(tex);
                    return string.IsNullOrEmpty(uri)
                        ? match.Value
                        : $"<img src='{uri}' class='math-img math-inline' />";
                }
            );

            // 4) Simple environment: \begin{...}…\end{...}
            content = await ProcessPattern(
                content,
                @"\\begin\{(\w+)\}(.+?)\\end\{\1\}",
                async match =>
                {
                    // reconstruct the full block so that codecogs sees \begin{env}…\end{env}
                    var env = match.Groups[1].Value;
                    var body = match.Groups[2].Value;
                    var tex = $"\\begin{{{env}}}{body}\\end{{{env}}}";
                    var uri = await MathConverter.ToSvgDataUriAsync(tex);
                    return string.IsNullOrEmpty(uri)
                        ? match.Value
                        : $"<img src='{uri}' class='math-img math-display' />";
                }
            );

            return content;
        }

        private static async Task<string> ProcessPattern(
            string input,
            string pattern,
            Func<Match, Task<string>> replacementFn)
        {
            var regex = new Regex(pattern, RegexOptions.Singleline);
            var sb = new StringBuilder();
            var last = 0;

            foreach (Match m in regex.Matches(input))
            {
                sb.Append(input, last, m.Index - last);
                sb.Append(await replacementFn(m));
                last = m.Index + m.Length;
            }

            sb.Append(input, last, input.Length - last);
            return sb.ToString();
        }
    }
}