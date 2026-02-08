namespace sicilBotApp.Models
{
    /// <summary>
    /// Gazete arama yöntemlerini tanýmlar
    /// </summary>
    public enum SearchMethod
    {
        ExactRegisterNumber = 1,      // Sicil No Eþit
        SimilarRegisterNumber = 2,    // Sicil No Benzer
        ExactName = 3,                // Ad Eþit
        Liquidation = 4,              // Tasfiye
        Bankruptcy = 5,               // Ýflas
        BankruptcyAlternative = 6     // Ýflas (Alternatif format)
    }
}