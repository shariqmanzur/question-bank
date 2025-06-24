using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuestionBank.Models
{
    public class Theme
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Theme name is required")]
        [StringLength(100, ErrorMessage = "Theme name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();

        public bool IsActive { get; set; } = true;

        // Tracking fields
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
