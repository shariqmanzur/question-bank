using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuestionBank.Models
{
    public class CompetencyLevel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Competency level is required")]
        [StringLength(10, ErrorMessage = "Competency level cannot exceed 10 characters")]
        public string Level { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

        public bool IsActive { get; set; } = true;

        // Tracking fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
