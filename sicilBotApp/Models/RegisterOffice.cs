namespace sicilBotApp.Models
{
    /// <summary>
    /// Ticaret Sicil Müdürlüðü bilgilerini temsil eder
    /// </summary>
    public class RegisterOffice
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public bool IsActive { get; set; } = true;

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }
}