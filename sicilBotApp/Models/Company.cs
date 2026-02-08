namespace sicilBotApp.Models
{
    /// <summary>
    /// Firma bilgilerini temsil eder
    /// </summary>
    public class Company
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string RegisterOfficeCode { get; set; } = string.Empty;
        public string? IdentityNumber { get; set; }
        public bool IsTaxNumber { get; set; }
        public DateTime? RegisterDate { get; set; }
        public DateTime? LastNfDate { get; set; }
        public bool IsValidIdentity { get; set; }

        public bool HasRegisterInfo()
        {
            return !string.IsNullOrEmpty(RegisterNumber) 
                   && !string.IsNullOrEmpty(RegisterOfficeCode);
        }

        public bool IsLiquidation()
        {
            return FullName.Contains("TASFÝYE", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsBankruptcy()
        {
            return FullName.Contains("ÝFLAS", StringComparison.OrdinalIgnoreCase);
        }
    }
}