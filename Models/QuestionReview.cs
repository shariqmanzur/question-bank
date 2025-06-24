using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuestionBank.Models
{
    public class QuestionReview
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }

        [ValidateNever]
        public virtual Question Question { get; set; }
        public string Comment { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
