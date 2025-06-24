namespace QuestionBank.Models
{
    public class PaperFilter
    {
        public int Id { get; set; }
        public string Title { get; set; }  // e.g., "Midterm Paper Filter"

        // Tracking fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }

        // Filters
        public int? ThemeId { get; set; }
        public int? TopicId { get; set; }
        public int? CompetencyLevelId { get; set; }
        public string? TeacherId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // JSON serialized version of SelectedQuestionTypesWithCount
        public string? TypeCountJson { get; set; }
    }
}
