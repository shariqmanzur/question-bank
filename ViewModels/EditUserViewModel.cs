using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuestionBank.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required, EmailAddress]
        [Remote(
        action: "IsEmailInUse",
        controller: "UserManagement",
        AdditionalFields = nameof(Id),
        ErrorMessage = "This email is already registered"
        )]
        public string Email { get; set; }

        [Required(ErrorMessage = "A role is required")]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
    }
}
