

    
    using System.Threading.Tasks;

    public class ExtractionAppService
    {
        private readonly ILLMProvider _llmProvider;

        public ExtractionAppService(ILLMProvider llmProvider)
        {
            _llmProvider = llmProvider;
        }

        public async Task<ExtractionResult> ExecuteExtractionAsync(GazetteQueryCriteria criteria)
        {
            // Business validations (e.g., checking if text is too short)
            if (criteria == null || string.IsNullOrWhiteSpace(criteria.RawGazetteText))
                return null;

            return await _llmProvider.ExtractDataAsync(criteria);
        }
    }
