using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QuestionBank.Models
{
    public class Topic
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Topic name is required")]
        [StringLength(100, ErrorMessage = "Topic name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [ForeignKey("Theme")]
        [Required(ErrorMessage = "Theme is required")]
        public int ThemeId { get; set; }

        [ValidateNever]
        public virtual Theme Theme { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

        public bool IsActive { get; set; } = true;

        // Tracking fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
