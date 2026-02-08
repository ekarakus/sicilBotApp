namespace sicilBotApp.Models
{
    /// <summary>
    /// Arama sonucunu temsil eder
    /// </summary>
    public class SearchResult
    {
        public Company Company { get; set; } = new();
        public List<Gazette> Gazettes { get; set; } = new();
        public bool RegisterFound { get; set; }
        public bool HasNewGazette { get; set; }
        public DateTime? NewestGazetteDate { get; set; }
        public SearchMethod? SuccessfulMethod { get; set; }
        public bool WasScanned { get; set; }
        public DateTime ScanDate { get; set; } = DateTime.Now;

        public int NewGazetteCount => Gazettes.Count(g => 
            Company.RegisterDate.HasValue && g.IsPublishedAfter(Company.RegisterDate));

        public Gazette? GetNewestGazette()
        {
            return Gazettes.OrderByDescending(g => g.PublishDate).FirstOrDefault();
        }
    }
}