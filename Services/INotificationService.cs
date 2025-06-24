namespace QuestionBank.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification to a specific recipient.
        /// </summary>
        /// <param name="recipientUserId">The recipient's username (email or identifier).</param>
        /// <param name="message">The message to be sent.</param>
        /// <param name="questionId">Optional question ID related to the notification.</param>
        Task SendNotificationAsync(string recipientUserId, string message, int? questionId = null);
    }
}
