namespace sicilBotApp.DTOs
{
    public class GazetteInfo
    {
        public string RegisterOffice { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string CompanyTitle { get; set; } = string.Empty;
      
        public DateTime PublishDate { get; set; }
        public string PublicationDate { get; set; } = string.Empty;
        public string IssueNumber { get; set; } = string.Empty;
        public string PageNumber { get; set; } = string.Empty;
        public string AnnouncementType { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty;
        public string VisitUrl { get; set; } = string.Empty;
        public string PdfText { get; set; } = string.Empty;
        public string AnnouncementId { get; set; } = string.Empty;
    }
}