namespace sicilBotApp.DTOs
{
    public class CompanySearchRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string RegisterOffice { get; set; } = string.Empty;
        public string? ManualCaptcha { get; set; }
    }
}