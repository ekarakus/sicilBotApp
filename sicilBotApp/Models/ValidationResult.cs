namespace sicilBotApp.Models
{
    /// <summary>
    /// Validasyon sonucunu temsil eder
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public string GetErrorMessage()
        {
            return string.Join(", ", Errors);
        }
    }
}