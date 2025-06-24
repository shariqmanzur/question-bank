using QuestionBank.Data;
using QuestionBank.Models;
using System;
using System.Threading.Tasks;

namespace QuestionBank.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(string recipientUserId, string message, int? questionId = null)
        {
            var notification = new Notification
            {
                RecipientUserId = recipientUserId,
                Message = message,
                QuestionId = questionId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
    }
}
