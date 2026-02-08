using System.Net;

namespace sicilBotApp.Infrastructure
{
    public interface IHttpClientWrapper
    {
        Task<string> GetStringAsync(string url);
        Task<byte[]> GetByteArrayAsync(string url);
        Task<HttpResponseData> PostMultipartAsync(string url, Dictionary<string, string> parameters);
        Task<string> DownloadPdfTextAsync(string url);
        CookieContainer Cookies { get; }
        public void SaveSession();
                 public void LoadSession();
    }

    public class HttpResponseData
    {
        public string? Content { get; set; }
        public Uri? ResponseUrl { get; set; }
    }
}