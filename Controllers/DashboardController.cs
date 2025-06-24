using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using QuestionBank.ViewModels.Dashboard;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace QuestionBank.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Detailed()
        {
            if (!User.IsInRole("Admin"))
            {
                // Teacher Dashboard: Filter data by the teacher's identity.
                var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier); // This is the ID
                var teacherUserName = await _context.Users
                    .Where(u => u.Id == teacherId)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync();

                var questionsQuery = _context.Questions
                    .Where(q => q.CreatedBy == teacherId);

                // Overall counts
                var totalQuestions = await questionsQuery.CountAsync();
                var acceptedQuestions = await questionsQuery
                                          .Where(q => q.Status == ReviewStatus.Accepted)
                                          .CountAsync();
                var pendingQuestions = await questionsQuery
                                          .Where(q => q.Status == ReviewStatus.Pending)
                                          .CountAsync();
                var rejectedQuestions = await questionsQuery
                                          .Where(q => q.Status == ReviewStatus.Rejected)
                                          .CountAsync();
                var totalReviews = await _context.QuestionReviews
                                          .Where(qr => qr.Question.CreatedBy == teacherUserName)
                                          .CountAsync();
                var totalTopics = await _context.Topics
                                          .Where(t => t.Questions.Any(q => q.CreatedBy == teacherUserName))
                                          .CountAsync();

                // Advanced Metrics
                var reviewedQuestions = await questionsQuery
                    .Where(q => q.Status == ReviewStatus.Accepted || q.Status == ReviewStatus.Rejected)
                    .ToListAsync();

                double avgReviewTime = reviewedQuestions.Any()
                    ? reviewedQuestions.Average(q => (q.ModifiedAt.Value - q.CreatedAt).TotalHours)
                    : 0;

                double acceptanceRatio = (acceptedQuestions + rejectedQuestions) > 0
                    ? (double)acceptedQuestions / (acceptedQuestions + rejectedQuestions)
                    : 0;

                var monthlyTrends = await questionsQuery
                    .GroupBy(q => new { q.CreatedAt.Year, q.CreatedAt.Month })
                    .Select(g => new MonthlyTrendViewModel
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(mt => mt.Year).ThenBy(mt => mt.Month)
                    .ToListAsync();

                // Analytics: Only Accepted questions
                var acceptedOnlyQuery = questionsQuery
                    .Where(q => q.Status == ReviewStatus.Accepted);

                // Themes & Topics analytics, filter on Accepted and only show those with data
                var themes = await _context.Themes
                    .Include(t => t.Topics)
                        .ThenInclude(topic => topic.Questions)
                    .ToListAsync();

                var themeAnalytics = themes
                    .Select(theme => new ThemeAnalyticsViewModel
                    {
                        ThemeName = theme.Name,
                        QuestionCount = theme.Topics
                                          .SelectMany(t => t.Questions)
                                          .Count(q => q.CreatedBy == teacherUserName
                                                   && q.Status == ReviewStatus.Accepted),
                        Topics = theme.Topics.Select(topic => new TopicAnalyticsViewModel
                        {
                            TopicName = topic.Name,
                            QuestionCount = topic.Questions
                                              .Count(q => q.CreatedBy == teacherUserName
                                                       && q.Status == ReviewStatus.Accepted)
                        })
                        .Where(t => t.QuestionCount > 0)
                        .ToList()
                    })
                    .Where(t => t.QuestionCount > 0)
                    .ToList();

                // Questions by Type, Accepted only
                var questionTypeAnalytics = await acceptedOnlyQuery
                    .GroupBy(q => q.QuestionType)
                    .Select(g => new QuestionTypeAnalyticsViewModel
                    {
                        QuestionType = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Competency Level analytics, Accepted only
                var competencyAnalytics = await acceptedOnlyQuery
                    .GroupBy(q => q.CompetencyLevel.Level)
                    .Select(g => new CompetencyAnalyticsViewModel
                    {
                        CompetencyLevel = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Teacher-specific summary
                var teacherAnalytics = new TeacherAnalyticsViewModel
                {
                    TeacherName = teacherUserName, // So this shows the Teacher Name Intead of ID
                    TotalQuestions = totalQuestions,
                    Accepted = acceptedQuestions,
                    Pending = pendingQuestions,
                    Rejected = rejectedQuestions,
                    TotalReviews = totalReviews
                };

                var model = new DetailedDashboardViewModel
                {
                    TotalQuestions = totalQuestions,
                    AcceptedQuestions = acceptedQuestions,
                    PendingQuestions = pendingQuestions,
                    RejectedQuestions = rejectedQuestions,
                    TotalReviews = totalReviews,
                    AverageReviewTime = avgReviewTime,
                    AcceptanceRatio = acceptanceRatio,
                    MonthlyTrends = monthlyTrends,
                    Themes = themeAnalytics,
                    QuestionTypes = questionTypeAnalytics,
                    CompetencyLevels = competencyAnalytics,
                    TeacherAnalytics = new List<TeacherAnalyticsViewModel> { teacherAnalytics }
                };

                return View(model);
            }
            else
            {
                // Admin Dashboard: Global analytics (unchanged)
                var totalQuestions = await _context.Questions.CountAsync();
                var acceptedQuestions = await _context.Questions
                                          .Where(q => q.Status == ReviewStatus.Accepted)
                                          .CountAsync();
                var pendingQuestions = await _context.Questions
                                          .Where(q => q.Status == ReviewStatus.Pending)
                                          .CountAsync();
                var rejectedQuestions = await _context.Questions
                                          .Where(q => q.Status == ReviewStatus.Rejected)
                                          .CountAsync();
                var totalReviews = await _context.QuestionReviews.CountAsync();
                var totalTopics = await _context.Topics.CountAsync();

                var reviewedQuestions = await _context.Questions
                    .Where(q => q.Status == ReviewStatus.Accepted || q.Status == ReviewStatus.Rejected)
                    .ToListAsync();

                double avgReviewTime = reviewedQuestions.Any()
                    ? reviewedQuestions.Average(q => (q.ModifiedAt.Value - q.CreatedAt).TotalHours)
                    : 0;

                double acceptanceRatio = (acceptedQuestions + rejectedQuestions) > 0
                    ? (double)acceptedQuestions / (acceptedQuestions + rejectedQuestions)
                    : 0;

                var monthlyTrends = await _context.Questions
                    .GroupBy(q => new { q.CreatedAt.Year, q.CreatedAt.Month })
                    .Select(g => new MonthlyTrendViewModel
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(mt => mt.Year).ThenBy(mt => mt.Month)
                    .ToListAsync();

                var themes = await _context.Themes
                    .Include(t => t.Topics)
                        .ThenInclude(topic => topic.Questions)
                    .ToListAsync();

                var themeAnalytics = themes.Select(theme => new ThemeAnalyticsViewModel
                {
                    ThemeName = theme.Name,
                    QuestionCount = theme.Topics.SelectMany(t => t.Questions).Count(),
                    Topics = theme.Topics.Select(topic => new TopicAnalyticsViewModel
                    {
                        TopicName = topic.Name,
                        QuestionCount = topic.Questions.Count
                    }).ToList()
                }).ToList();

                var questionTypeAnalytics = await _context.Questions
                    .GroupBy(q => q.QuestionType)
                    .Select(g => new QuestionTypeAnalyticsViewModel
                    {
                        QuestionType = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                var competencyAnalytics = await _context.Questions
                    .GroupBy(q => q.CompetencyLevel.Level)
                    .Select(g => new CompetencyAnalyticsViewModel
                    {
                        CompetencyLevel = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var teacherAnalytics = await _context.Questions
                    .Where(q => q.CreatedBy != null)
                    .GroupBy(q => q.CreatedBy)
                    .Select(g => new TeacherAnalyticsViewModel
                    {
                        TeacherName = _context.Users
                            .Where(u => u.Id == g.Key)
                            .Select(u => u.UserName) // Or u.FullName, u.Email if preferred
                            .FirstOrDefault(),

                        TotalQuestions = g.Count(),
                        Accepted = g.Count(q => q.Status == ReviewStatus.Accepted),
                        Pending = g.Count(q => q.Status == ReviewStatus.Pending),
                        Rejected = g.Count(q => q.Status == ReviewStatus.Rejected),
                        TotalReviews = g.SelectMany(q => q.QuestionReviews).Count()
                    })
                    .ToListAsync();


                var model = new DetailedDashboardViewModel
                {
                    TotalQuestions = totalQuestions,
                    AcceptedQuestions = acceptedQuestions,
                    PendingQuestions = pendingQuestions,
                    RejectedQuestions = rejectedQuestions,
                    TotalReviews = totalReviews,
                    AverageReviewTime = avgReviewTime,
                    AcceptanceRatio = acceptanceRatio,
                    MonthlyTrends = monthlyTrends,
                    Themes = themeAnalytics,
                    QuestionTypes = questionTypeAnalytics,
                    CompetencyLevels = competencyAnalytics,
                    TeacherAnalytics = teacherAnalytics
                };

                return View(model);
            }
        }
    }
}
