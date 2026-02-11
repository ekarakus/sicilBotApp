    
    using System.Threading.Tasks;

    /// <summary>
    /// LLM sağlayıcıları için standart kontrat.
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// Ham gazete metninden belirli kriterlere göre veriyi ayıklar.
        /// </summary>
        /// <param name="criteria">Arama kriterleri ve ham metin.</param>
        /// <returns>Ayıklanmış sonuç nesnesi.</returns>
        Task<ExtractionResult> ExtractDataAsync(GazetteQueryCriteria criteria);
    }
