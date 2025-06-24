namespace QuestionBank.Models
{
    public class PaperQuestion
    {
        // composite key of (PaperId, QuestionId)
        public int PaperId { get; set; }
        public Paper Paper { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}
