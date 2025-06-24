using System.ComponentModel.DataAnnotations;

namespace QuestionBank.Models
{
    public class Paper
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        // — “university” fields —
        [Required, Display(Name = "University Name"), StringLength(200)]
        public string UniversityName { get; set; }

        [Required, Display(Name = "Campus Name"), StringLength(200)]
        public string CampusName { get; set; }

        [Required, Display(Name = "Department"), StringLength(200)]
        public string Department { get; set; }

        [Required, Display(Name = "Course Name"), StringLength(200)]
        public string CourseName { get; set; }

        [Required, Display(Name = "Course Code"), StringLength(50)]
        public string CourseCode { get; set; }

        [Required, Display(Name = "Semester"), StringLength(50)]
        public string Semester { get; set; }

        [Required, Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; }

        [Required, Display(Name = "Duration (hh:mm)")]
        public TimeSpan Duration { get; set; }

        [Required, Display(Name = "Total Marks")]
        public int TotalMarks { get; set; }

        [Display(Name = "Instructions"), DataType(DataType.MultilineText)]
        public string Instructions { get; set; }

        // logging
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsArchived { get; set; }

        // questions
        public ICollection<PaperQuestion> PaperQuestions { get; set; } = new List<PaperQuestion>();
    }
}
