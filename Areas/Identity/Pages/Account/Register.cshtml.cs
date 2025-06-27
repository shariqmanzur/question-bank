using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace QuestionBank.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [EmailAddress]
            [Display(Name = "Email (optional)")]
            public string? Email { get; set; }

            [Required]
            [Display(Name = "Username")]
            [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers.")]
            public string UserName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        // after:
        public async Task OnGetAsync(
            string returnUrl = null,
            string loginUserID = null,   // ← new
            string password = null)      // ← new
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // seed the form if query-string provided
            Input = new InputModel
            {
                UserName = string.IsNullOrWhiteSpace(loginUserID)
                                  ? null
                                  : loginUserID,
                Password = string.IsNullOrWhiteSpace(password)
                                  ? null
                                  : password,
                ConfirmPassword = string.IsNullOrWhiteSpace(password)
                                  ? null
                                  : password
                // leave Email alone (optional)
            };
        }

        // Razor-Pages handler for /Register?handler=VerifyUsername&userName=...
        public async Task<JsonResult> OnGetVerifyUsernameAsync(string userName)
        {
            var exists = await _userManager.FindByNameAsync(userName) != null;
            return new JsonResult(!exists);
        }

        // Razor-Pages handler for /Register?handler=VerifyEmail&email=...
        public async Task<JsonResult> OnGetVerifyEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new JsonResult(true);

            var exists = await _userManager.FindByEmailAsync(email) != null;
            return new JsonResult(!exists);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(Input.Email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Input.Email", "Email is already in use.");
                        return Page();
                    }
                }

                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);

                if (!string.IsNullOrEmpty(Input.Email))
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    await _userManager.AddToRoleAsync(user, "Teacher");

                    if (!string.IsNullOrEmpty(Input.Email))
                    {
                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId, code, returnUrl },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(
                            Input.Email,
                            "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount && !string.IsNullOrEmpty(Input.Email))
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try { return Activator.CreateInstance<IdentityUser>(); }
            catch
            {
                throw new InvalidOperationException(
                  $"Can't create an instance of '{nameof(IdentityUser)}'.");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("This app requires a user store with email support.");
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}