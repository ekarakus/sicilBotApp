using Docnet.Core;
using Docnet.Core.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;

namespace sicilBotApp.Infrastructure
{
    public class HttpClientWrapper : IHttpClientWrapper
    {
        
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private const string BaseUrl = "https://www.ticaretsicil.gov.tr/";
        private readonly string _sessionFilePath = Path.Combine(AppContext.BaseDirectory, "session.json");
        private readonly string _tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        private readonly ICustomLogger _logger;

        public CookieContainer Cookies => _cookieContainer;

        public HttpClientWrapper()
        {//aþaðýda ne yapmam gerekir?
            _logger = new ConsoleLogger();
            // Logger'ý burada da kullanmak için örnek oluþturuyoruz
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true,
                UseCookies = true
            };
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

            // Tarayýcý gibi görünmek için User-Agent ekliyoruz
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<string> GetStringAsync(string url) => await _httpClient.GetStringAsync(url);

        public async Task<byte[]> GetByteArrayAsync(string url) => await _httpClient.GetByteArrayAsync(url);

        public async Task<HttpResponseData> PostMultipartAsync(string url, Dictionary<string, string> parameters)
        {
            using var content = new MultipartFormDataContent();
            foreach (var param in parameters)
            {
                content.Add(new StringContent(param.Value), param.Key);
            }

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new HttpResponseData
            {
                Content = responseContent.Trim(),
                ResponseUrl = response.RequestMessage?.RequestUri
            };
        }

        public async Task<string> DownloadPdfTextAsync(string url)
        {
            try
            {
                // 1. Sayfa içeriðinden PDF linkini bul
                var htmlContent = await GetStringAsync(url);
                var pdfPath = ExtractPdfUrlFromHtml(htmlContent);

                if (string.IsNullOrWhiteSpace(pdfPath))
                    throw new Exception("Sayfa içerisinde PDF dökümaný bulunamadý.");

                // 2. PDF dosyasýný indir
                var absolutePdfUrl = pdfPath.StartsWith("http") ? pdfPath : new Uri(new Uri(BaseUrl), pdfPath).ToString();
                var pdfBytes = await GetByteArrayAsync(absolutePdfUrl);

                // 3. OCR ve Metin Ayýklama iþlemini baþlat
                return await ExtractTextFromPdfWithOcr(pdfBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF Ýþleme Hatasý: {ex.Message}", ex);
            }
        }

        private async Task<string> ExtractTextFromPdfWithOcr(byte[] pdfBytes)
        {
            var textBuilder = new StringBuilder();

            // Önce iText 9.5.0 ile dijital metin katmaný var mý kontrol et
            using (var reader = new PdfReader(new MemoryStream(pdfBytes)))
            using (var pdfDoc = new PdfDocument(reader))
            {
                var strategy = new LocationTextExtractionStrategy();
                var pageCount = pdfDoc.GetNumberOfPages();

                // Ýlk sayfada metin yoðunluðu kontrolü
                var firstPageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(1), strategy);
                if (!string.IsNullOrWhiteSpace(firstPageText) && firstPageText.Length > 100)
                {
                    for (int i = 1; i <= pageCount; i++)
                        textBuilder.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy));

                    return textBuilder.ToString();
                }
            }

            // Metin katmaný yoksa Docnet.Core 2.6.0 ve Tesseract 5.2.0 ile OCR yap
            using var library = DocLib.Instance;
            // 2.0f ölçeði Mersis No gibi küçük verilerin netliði için kritiktir
            using var docReader = library.GetDocReader(pdfBytes, new PageDimensions(2.0f));
            using var engine = new TesseractEngine(_tessdataPath, "tur+eng", EngineMode.Default);

            for (int i = 0; i < docReader.GetPageCount(); i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage(); // BGR formatýnda ham piksel verisi
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();

                // System.Drawing.Common 8 ile Bitmap oluþturma ve piksel transferi
                using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb))
                {
                    var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
                    bitmap.UnlockBits(bmpData);

                    // PixConverter hatasýný aþmak için bellek üzerinden Pix yükleme
                    using (var ms = new MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                        {
                            using (var ocrPage = engine.Process(pix))
                            {
                                textBuilder.AppendLine($"--- Sayfa {i + 1} ---");
                                textBuilder.AppendLine(ocrPage.GetText());
                            }
                        }
                    }
                }
            }

            return textBuilder.ToString().Trim();
        }

        private string ExtractPdfUrlFromHtml(string html)
        {
            var patterns = new[]
            {
                @"<object[^>]+data=[""'](?<url>[^""']+\.pdf)[""']",
                @"<embed[^>]+src=[""'](?<url>[^""']+\.pdf)[""']",
                @"<iframe[^>]+src=[""'](?<url>[^""']+\.pdf)[""']"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups["url"].Value;
            }
            return string.Empty;
        }

        public void SaveSession()
        {
            try
            {
                var uri = new Uri(BaseUrl);
                // CookieContainer'dan tüm çerezleri alýyoruz (Domain bazlý)
                var cookies = _cookieContainer.GetCookies(uri).Cast<Cookie>().ToList();
                var json = JsonSerializer.Serialize(cookies);
                File.WriteAllText(_sessionFilePath, json);
            }
            catch (Exception ex) { /* Loglama */ }
        }

        public void LoadSession()
        {
            if (!File.Exists(_sessionFilePath)) return;
            try
            {
                var json = File.ReadAllText(_sessionFilePath);
                var cookies = JsonSerializer.Deserialize<List<Cookie>>(json);
                if (cookies != null)
                {
                    var uri = new Uri(BaseUrl);
                    foreach (var cookie in cookies)
                    {
                        _cookieContainer.Add(uri, cookie);
                    }
                }
            }
            catch (Exception ex) { /* Loglama */ }
        }
    }

    
}