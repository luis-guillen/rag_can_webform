using System.Linq;

namespace rag_can_aspx.Services
{
    public enum Quality { Empty, LowQuality, Ok }

    public static class QualityScorer
    {
        private const int MinEmpty = 50;
        private const int MinLow = 300;

        public static Quality Score(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Quality.Empty;

            string clean = text.Trim();
            if (clean.Length < MinEmpty)
                return Quality.Empty;

            if (clean.Length < MinLow)
                return Quality.LowQuality;

            return Quality.Ok;
        }

        public static string ToLabel(Quality q)
        {
            switch (q)
            {
                case Quality.Empty: return "empty";
                case Quality.LowQuality: return "low_quality";
                default: return "ok";
            }
        }
    }
}
