namespace QuestionBank.ViewModels
{
    public class PaperSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string UniversityName { get; set; } = "";
        public string CampusName { get; set; } = "";
        public string Department { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string CourseCode { get; set; } = "";
        public string Semester { get; set; } = "";
        public DateTime ExamDate { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalMarks { get; set; }
        public string Instructions { get; set; } = "";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsArchived { get; set; }
        public string DurationFormatted =>
            string.Join(" ",
                (Duration.Hours > 0
                    ? $"{Duration.Hours} hour{(Duration.Hours > 1 ? "s" : "")}"
                    : null),
                (Duration.Minutes > 0
                    ? $"{Duration.Minutes} minute{(Duration.Minutes > 1 ? "s" : "")}"
                    : null)
            ).Trim() switch
            {
                "" => "0 minutes",
                var s => s
            };
    }
}