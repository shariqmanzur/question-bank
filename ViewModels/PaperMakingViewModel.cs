// ViewModels/PaperMakingViewModel.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestionBank.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuestionBank.ViewModels
{
    public class PaperMakingViewModel : IValidatableObject
    {
        // —— FILTER PROPS (skip all validation) ——
        [ValidateNever] public int? SelectedThemeId { get; set; }
        [ValidateNever] public int? SelectedTopicId { get; set; }
        [ValidateNever] public int? SelectedCompetencyLevelId { get; set; }
        [ValidateNever] public string? SelectedTeacherId { get; set; }
        [ValidateNever] public string SelectedQuestionType { get; set; }
        [ValidateNever] public DateTime? StartDate { get; set; }
        [ValidateNever] public DateTime? EndDate { get; set; }
        [ValidateNever] public bool FilterApplied { get; set; }
        [ValidateNever] public Dictionary<string, int> SelectedQuestionTypesWithCount { get; set; } = new();

        public int? PaperFilterId { get; set; }
        public List<SelectListItem>? PaperFilters { get; set; }
        public string? NewFilterTitle { get; set; }


        // —— DROPDOWNS (skip validation) ——
        [ValidateNever] public List<Theme> Themes { get; set; }
        [ValidateNever] public List<Topic> AvailableTopics { get; set; } = new();
        [ValidateNever] public List<CompetencyLevel> CompetencyLevels { get; set; }
        [ValidateNever] public List<SelectListItem> Teachers { get; set; }
        [ValidateNever] public List<SelectListItem> QuestionTypes { get; set; } = new();

        // —— QUESTIONS (skip validation) ——        
        [ValidateNever] public List<Question> AvailableQuestions { get; set; } = new();
        [ValidateNever] public List<int> SelectedQuestionIds { get; set; } = new();

        // —— PAPER METADATA ——

        [Required(ErrorMessage = "Paper title is required")]
        [Display(Name = "Paper Title")]
        public string PaperTitle { get; set; }

        [Required(ErrorMessage = "University name is required")]
        [Display(Name = "University Name")]
        public string UniversityName { get; set; } = "ILMA UNIVERSITY";

        [Required(ErrorMessage = "Campus name is required")]
        [StringLength(200, ErrorMessage = "Campus name must not exceed 200 characters")]
        [Display(Name = "Campus Name")]
        public string CampusName { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Course name is required")]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Course code is required")]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; }

        [Required(ErrorMessage = "Semester is required")]
        [Display(Name = "Semester")]
        public string Semester { get; set; }

        [Required(ErrorMessage = "Exam date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Display(Name = "Duration")]
        public string Duration { get; set; } // Format "HH:mm"

        [ValidateNever]
        public double? DurationInHours { get; set; } // Calculated from Duration

        [Required(ErrorMessage = "Total marks is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Total marks must be greater than zero")]
        [Display(Name = "Total Marks")]
        public int TotalMarks { get; set; }

        [Required(ErrorMessage = "Instructions is required")]
        [Display(Name = "Instructions")]
        [DataType(DataType.MultilineText)]
        public string Instructions { get; set; } // contains HTML + MathML/LaTeX snippets

        // Custom validation for ExamDate:
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ExamDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "The exam date cannot be in the past",
                    new[] { nameof(ExamDate) }
                );
            }
        }
    }
}