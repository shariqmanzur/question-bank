using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuestionBank.Models
{
    public class BCQAnswer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Answer text is required")]
        [StringLength(500, ErrorMessage = "Answer text cannot exceed 500 characters")]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        [ForeignKey("Question")]
        [Required(ErrorMessage = "Associated question is required")]
        public int QuestionId { get; set; }

        [ValidateNever]
        public virtual Question Question { get; set; }
    }
}
