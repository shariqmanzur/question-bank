using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuestionBank.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int? QuestionId { get; set; } // Make it nullable

        [ValidateNever]
        public virtual Question? Question { get; set; }

        public string RecipientUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}