using VaderSharp;

namespace SideStream.Common
{
    public static class ChatProcessingUtils
    {
        private static readonly SentimentIntensityAnalyzer SentimentAnalyzer = new SentimentIntensityAnalyzer();

        public static double AnalyzeSentiment(string message)
        {
            var results = SentimentAnalyzer.PolarityScores(message);
            return results.Compound;
        }
    }
}