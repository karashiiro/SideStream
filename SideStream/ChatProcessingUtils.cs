using VaderSharp;

namespace SideStream
{
    internal static class ChatProcessingUtils
    {
        private static readonly SentimentIntensityAnalyzer SentimentAnalyzer = new SentimentIntensityAnalyzer();

        public static SentimentAnalysisResults AnalyzeSentiment(string message)
            => SentimentAnalyzer.PolarityScores(message);
    }
}