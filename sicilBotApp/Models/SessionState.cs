namespace sicilBotApp.Models
{
    /// <summary>
    /// Uygulama oturum durumunu temsil eder
    /// </summary>
    public class SessionState
    {
        public bool IsAuthenticated { get; set; }
        public DateTime? LoginTime { get; set; }
        public string? SessionId { get; set; }
        public string? UserEmail { get; set; }
        public int RequestCount { get; set; }
        public DateTime? LastRequestTime { get; set; }

        public bool IsExpired(int timeoutMinutes = 30)
        {
            return LoginTime.HasValue 
                   && DateTime.Now.Subtract(LoginTime.Value).TotalMinutes > timeoutMinutes;
        }

        public bool ShouldThrottle(int maxRequests = 100, int windowMinutes = 1)
        {
            if (!LastRequestTime.HasValue) return false;

            var timeSinceLastRequest = DateTime.Now.Subtract(LastRequestTime.Value);
            
            if (timeSinceLastRequest.TotalMinutes > windowMinutes)
            {
                RequestCount = 0;
                return false;
            }

            return RequestCount >= maxRequests;
        }

        public void RecordRequest()
        {
            RequestCount++;
            LastRequestTime = DateTime.Now;
        }
    }
}