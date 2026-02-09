namespace sicilBotApp.Services
{
    public interface IGazetteSearchService
    {
        Task<DTOs.ApiResponse<List<DTOs.GazetteInfo>>> SearchGazettesAsync(DTOs.CompanySearchRequest request);
        Task<string> GetGazetteTextAsync(string gazetteUrl);
        Task<byte[]> GetGazettePdfAsync(string pdfUrl);
    }
}