namespace sicilBotApp.Models
{
    /// <summary>
    /// Olumsuz ödeme kaydý bilgilerini temsil eder
    /// </summary>
    public class NegativePaymentRecord
    {
        public string IdentityNumber { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public DateTime? RecordDate { get; set; }
        public bool HasNewRecord { get; set; }
        public DateTime ScanDate { get; set; } = DateTime.Now;

        public bool IsNewerThan(DateTime? previousDate)
        {
            return RecordDate.HasValue 
                   && previousDate.HasValue 
                   && RecordDate.Value > previousDate.Value;
        }
    }
}