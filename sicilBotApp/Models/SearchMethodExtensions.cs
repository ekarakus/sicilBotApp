namespace sicilBotApp.Models
{
    public static class SearchMethodExtensions
    {
        public static string GetDescription(this SearchMethod method)
        {
            return method switch
            {
                SearchMethod.ExactRegisterNumber => "Sicil No (Eþit)",
                SearchMethod.SimilarRegisterNumber => "Sicil No (Benzer)",
                SearchMethod.ExactName => "Ünvan (Eþit)",
                SearchMethod.Liquidation => "Tasfiye Halinde",
                SearchMethod.Bankruptcy => "Ýflas Nedeniyle Tasfiye",
                SearchMethod.BankruptcyAlternative => "Ýflas (Alternatif)",
                _ => "Bilinmeyen Yöntem"
            };
        }

        public static string GetSearchPrefix(this SearchMethod method, string companyName)
        {
            return method switch
            {
                SearchMethod.Liquidation => $"TASFÝYE HALÝNDE {companyName}",
                SearchMethod.Bankruptcy => $"ÝFLAS NEDENÝYLE TASFÝYE HALÝNDE {companyName}",
                SearchMethod.BankruptcyAlternative => $"(ÝFLAS NEDENÝYLE) TASFÝYE HALÝNDE {companyName}",
                _ => companyName
            };
        }
    }
}