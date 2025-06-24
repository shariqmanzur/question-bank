namespace QuestionBank.ViewModels.Dashboard
{
    public class DetailedDashboardViewModel
    {
        public int TotalQuestions { get; set; }
        public int AcceptedQuestions { get; set; }
        public int PendingQuestions { get; set; }
        public int RejectedQuestions { get; set; }
        public int TotalReviews { get; set; }
        public double AverageReviewTime { get; set; } // In hours.
        public double AcceptanceRatio { get; set; }   // Percentage or ratio.
        public List<MonthlyTrendViewModel> MonthlyTrends { get; set; }
        public List<ThemeAnalyticsViewModel> Themes { get; set; }
        public List<QuestionTypeAnalyticsViewModel> QuestionTypes { get; set; }
        public List<CompetencyAnalyticsViewModel> CompetencyLevels { get; set; }
        public List<TeacherAnalyticsViewModel> TeacherAnalytics { get; set; }
    }
}
