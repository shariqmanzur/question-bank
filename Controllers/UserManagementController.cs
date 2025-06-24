using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestionBank.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionBank.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: UserManagement/Index
        public async Task<IActionResult> Index()
        {
            var users = await Task.FromResult(_userManager.Users.AsNoTracking().ToList());
            return View(users);
        }

        // GET: UserManagement/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            ViewBag.PasswordInfo = "Password is stored securely and cannot be displayed in plain text";
            ViewBag.Role = role;

            return View(user);
        }

        // GET: UserManagement/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            var roles = await Task.FromResult(_roleManager.Roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToList());

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                SelectedRole = currentRole,
                Roles = roles
            };

            return View(model);
        }

        // POST: UserManagement/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            if (currentRole != model.SelectedRole)
            {
                if (!string.IsNullOrEmpty(currentRole))
                    await _userManager.RemoveFromRoleAsync(user, currentRole);

                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(model.SelectedRole);
                    if (roleExists)
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Selected role does not exist");
                        return View(model);
                    }
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.Roles = _roleManager.Roles.Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList();

                return View(model);
            }

            TempData["SuccessMessage"] = "User updated successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: UserManagement/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Error deleting user");
                return View(user);
            }

            TempData["SuccessMessage"] = "User deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> IsEmailInUse(string email, string id)
        {
            // Find any user with this email
            var user = await _userManager.FindByEmailAsync(email);

            // If no user, or it’s the same user we’re editing, it’s valid
            if (user == null || user.Id == id)
            {
                return Json(true);
            }

            // Otherwise respond with an error message
            return Json($"Email '{email}' is already in use");
        }
    }
}
