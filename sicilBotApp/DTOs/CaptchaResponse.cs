namespace sicilBotApp.DTOs
{
    public class CaptchaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CaptchaImageBase64 { get; set; }
        public string? AutoResolvedText { get; set; }
        public bool RequiresManualInput { get; set; }
        public bool IsCriticalError { get; set; }
    }
}