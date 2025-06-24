using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Data;
using QuestionBank.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuestionBank.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationViewComponent(ApplicationDbContext context)
            => _context = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // If not logged in, render empty
            if (User.Identity?.IsAuthenticated != true)
                return View(Enumerable.Empty<Notification>());

            // 1) Get current user's Identity ID
            var currentUserId = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2) Pull notifications where RecipientUserId == this ID
            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == currentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }
    }
}
