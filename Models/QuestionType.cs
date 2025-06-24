namespace QuestionBank.Models
{
    /// <summary>
    /// Defines the different types of questions in the Question Bank.
    /// </summary>
    public enum QuestionType
    {
        /// <summary> Default value for unknown question types. </summary>
        Unknown = 0,

        /// <summary> Best Choice Question (MCQ format). </summary>
        BCQ = 1,

        /// <summary> Short Answer Question. </summary>
        SAQ = 2,

        /// <summary> Image-Based Question. </summary>
        Image = 3
    }
}
