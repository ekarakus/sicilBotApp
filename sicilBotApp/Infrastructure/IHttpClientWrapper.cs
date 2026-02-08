namespace sicilBotApp.Infrastructure
{
    public interface IHttpClientWrapper
    {
        Task<string> GetStringAsync(string url);
        Task<byte[]> GetByteArrayAsync(string url);
        Task<HttpResponseData> PostMultipartAsync(string url, Dictionary<string, string> parameters);
        Task<string> DownloadPdfTextAsync(string url);
    }

    public class HttpResponseData
    {
        public string? Content { get; set; }
        public Uri? ResponseUrl { get; set; }
    }
}