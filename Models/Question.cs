using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuestionBank.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(1000, ErrorMessage = "Question text cannot exceed 1000 characters")]
        public string QuestionText { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Question type is required")]
        public QuestionType QuestionType { get; set; }

        [Required(ErrorMessage = "Competency level is required")]
        [ForeignKey("CompetencyLevel")]
        public int CompetencyLevelId { get; set; }

        [ValidateNever]
        public virtual CompetencyLevel CompetencyLevel { get; set; }

        [Required(ErrorMessage = "Topic is required")]
        [ForeignKey("Topic")]
        public int TopicId { get; set; }

        [ValidateNever]
        public virtual Topic Topic { get; set; }

        // For SAQ-specific answer
        [StringLength(1000, ErrorMessage = "Short answer cannot exceed 1000 characters")]
        public string? ShortAnswer { get; set; }

        public virtual ICollection<BCQAnswer> BCQAnswers { get; set; } = new List<BCQAnswer>();

        public bool IsActive { get; set; } = true;

        // Tracking fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        public virtual ICollection<QuestionReview> QuestionReviews { get; set; } = new List<QuestionReview>();
        
        // Add this collection property to relate notifications to a question.
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
