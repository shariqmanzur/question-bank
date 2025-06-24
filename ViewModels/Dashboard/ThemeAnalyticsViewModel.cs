namespace QuestionBank.ViewModels.Dashboard
{
    public class ThemeAnalyticsViewModel
    {
        public string ThemeName { get; set; }
        public int QuestionCount { get; set; }
        public List<TopicAnalyticsViewModel> Topics { get; set; }
    }
}
