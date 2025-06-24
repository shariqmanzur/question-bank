namespace QuestionBank.ViewModels.Dashboard
{
    public class TeacherAnalyticsViewModel
    {
        public string TeacherName { get; set; }
        public int TotalQuestions { get; set; }
        public int Accepted { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int TotalReviews { get; set; }
    }
}
