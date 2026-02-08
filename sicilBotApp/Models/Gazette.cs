namespace sicilBotApp.Models
{
    /// <summary>
    /// Ticaret Sicil Gazetesi kaydýný temsil eder
    /// </summary>
    public class Gazette
    {
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public string PageNumber { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Guid { get; set; }
        public int SimilarityScore { get; set; }
        public SearchMethod? MatchMethod { get; set; }

        public bool IsPublishedAfter(DateTime? date)
        {
            return date.HasValue && PublishDate > date.Value;
        }

        public bool IsRelevant(string companyName, int minimumScore = 40)
        {
            return SimilarityScore >= minimumScore;
        }
    }
}