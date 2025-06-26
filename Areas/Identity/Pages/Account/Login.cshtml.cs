using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace QuestionBank.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Email or Username")]
            public string EmailOrUsername { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        // Flags for server‐side ModelState errors
        public bool IsEmailInvalid =>
            ViewData.ModelState[nameof(Input.EmailOrUsername)]?.Errors.Count > 0;
        public bool IsPasswordInvalid =>
            ViewData.ModelState[nameof(Input.Password)]?.Errors.Count > 0;

        /// <summary>
        /// Renders the login form. Propagates any TempData error into the EmailOrUsername field.
        /// </summary>
        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(
                    nameof(Input.EmailOrUsername),
                    ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // clear any existing external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            ReturnUrl = returnUrl;
        }

        /// <summary>
        /// Handles the main POST login attempt.
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
            {
                // If validation failed, just redisplay the form
                return Page();
            }

            // 1) Find user by email or username
            IdentityUser user;
            if (Input.EmailOrUsername.Contains("@"))
            {
                user = await _signInManager.UserManager
                    .FindByEmailAsync(Input.EmailOrUsername);
            }
            else
            {
                user = await _signInManager.UserManager
                    .FindByNameAsync(Input.EmailOrUsername);
            }

            if (user == null)
            {
                ModelState.AddModelError(
                    nameof(Input.EmailOrUsername),
                    "User not found.");
                return Page();
            }

            // 2) Check password
            var result = await _signInManager
                .CheckPasswordSignInAsync(
                    user, Input.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, Input.RememberMe);
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new { ReturnUrl = returnUrl, Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User locked out.");
                return RedirectToPage("./Lockout");
            }

            // wrong password
            ModelState.AddModelError(
                nameof(Input.Password),
                "Incorrect password.");
            return Page();
        }

        /// <summary>
        /// AJAX: verifies that a user with this email or username exists.
        /// </summary>
        public async Task<JsonResult> OnGetVerifyUserAsync(string emailOrUsername)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
                return new JsonResult(false);

            IdentityUser user;
            if (emailOrUsername.Contains("@"))
                user = await _signInManager.UserManager.FindByEmailAsync(emailOrUsername);
            else
                user = await _signInManager.UserManager.FindByNameAsync(emailOrUsername);

            return new JsonResult(user != null);
        }

        /// <summary>
        /// AJAX: verifies that the given password is correct for the specified user.
        /// </summary>
        public async Task<JsonResult> OnGetVerifyPasswordAsync(string emailOrUsername, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername) || string.IsNullOrWhiteSpace(password))
                return new JsonResult(false);

            IdentityUser user;
            if (emailOrUsername.Contains("@"))
                user = await _signInManager.UserManager.FindByEmailAsync(emailOrUsername);
            else
                user = await _signInManager.UserManager.FindByNameAsync(emailOrUsername);

            if (user == null)
                return new JsonResult(false);

            var result = await _signInManager
                .CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            return new JsonResult(result.Succeeded);
        }
    }
}