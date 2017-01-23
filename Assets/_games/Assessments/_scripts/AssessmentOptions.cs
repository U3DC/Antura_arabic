using Kore.Utils;

namespace EA4S.Assessment
{
    /// <summary>
    /// Text flow options. I assume most languages are LeftToRight
    /// and Right To left, We may have to add further options in future
    /// if we need Chinese/Japanese. 
    /// </summary>
    public enum TextFlow
    {
        LeftToRight,
        RightToLeft
    }

    /// <summary>
    /// These options are setted by AssessmentFactory after MiniGameAPI produced the
    /// AssessmentConfiguration instance.
    /// </summary>
    public class AssessmentOptions
    {
        private static AssessmentOptions instance;
        public static AssessmentOptions Instance
        {
            get
            {
                return instance;
            }
        }

        public static void Reset()
        {
            instance = new AssessmentOptions();
        }

        public TextFlow LocaleTextFlow { get; set; }
        public bool PronunceQuestionWhenClicked { get; set; }
        public bool PronunceAnswerWhenClicked { get; set; }
        public bool ShowQuestionAsImage { get; set; }
        public bool PlayQuestionAudioAfterTutorial { get; set; }
        public bool QuestionAnsweredFlip { get; set; }
        public bool QuestionAnsweredPlaySound { get; set; }
        public bool QuestionSpawnedPlaySound { get; set; }
    }
}