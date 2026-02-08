namespace sicilBotApp.Models
{
    /// <summary>
    /// Tarama oturumunu temsil eder
    /// </summary>
    public class ScanSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public int TotalCompanies { get; set; }
        public int ScannedCompanies { get; set; }
        public int SuccessfulScans { get; set; }
        public int FailedScans { get; set; }
        public int NewGazettesFound { get; set; }
        public int NewNfRecordsFound { get; set; }
        public bool IsCompleted { get; set; }
        public bool WasCancelled { get; set; }

        public TimeSpan Duration => 
            (EndTime ?? DateTime.Now) - StartTime;

        public double SuccessRate => 
            ScannedCompanies > 0 
                ? (double)SuccessfulScans / ScannedCompanies * 100 
                : 0;

        public void Complete()
        {
            EndTime = DateTime.Now;
            IsCompleted = true;
        }

        public void Cancel()
        {
            EndTime = DateTime.Now;
            WasCancelled = true;
        }
    }
}