using Docnet.Core;
using Docnet.Core.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using sicilBotApp.DTOs;
using sicilBotApp.Services;
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

        
        
        
        
        private readonly string _tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
              


        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private readonly string _sessionFilePath = Path.Combine(AppContext.BaseDirectory, "session.json");
        private readonly ICustomLogger _logger;
        private const string BaseUrl = "https://www.ticaretsicil.gov.tr/";

        // Ayný anda sadece bir iþlemin dosyaya yazmasýný veya login olmasýný saðlar
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public CookieContainer Cookies => _cookieContainer;

        public HttpClientWrapper(ICustomLogger logger)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };

            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            LoadSession();
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

            if (IsSessionExpired(responseContent))
            {
                throw new UnauthorizedAccessException("Oturum süresi dolmuþ.");
            }

            return new HttpResponseData
            {
                Content = responseContent.Trim(),
                ResponseUrl = response.RequestMessage?.RequestUri
            };
        }
        // <summary>
        /// Verilen URL'deki PDF dosyasýný indirir, OCR ile metin çýkarýr ve sonucu döner.
        ///     </summary>
        public async Task<string> DownloadPdfTextAsync(string url)
        {
            // 1. pdf i indir
         var pdfBytes=   DownloadPdfAsync(url).Result;
            // 2. OCR ve Metin Ayýklama iþlemini baþlat
            return await ExtractTextFromPdfWithOcr(pdfBytes);
        }
        //<summary>
        // pdfurl'sini indir ve döndür
        //</summary>
        // DownloadPdfTextAsync
        //
        
        public async Task<byte[]> DownloadPdfAsync(string pdfUrl)
        {
            try
            {
                
                // Sayfa içeriðinden PDF linkini bul
                var htmlContent = await GetStringAsync(pdfUrl);

                // Eðer HTML içeriði login sayfasýna yönlendirdiyse, oturum dolmuþ demektir
                if (IsSessionExpired(htmlContent))
                {
                    _logger.LogWarning("Oturum süresi dolmuþ veya giriþ yapýlmamýþ.");
                    throw new UnauthorizedAccessException("Oturum süresi dolmuþ. Lütfen tekrar giriþ yapýn.");
                }

                //eðer içinde <form id="FormGuvenlikKodu">  kodu varsa içeriðe eriþmen için doðrulama kodunu girmen gerekiyor
                // demektir. Bu durumda da cpathca kodunu çöz.
                // <img  id="CaptchaImg" src="/captcha/captcha.php?1770637946"/> gibi bir kod var ise captcha var demektir.
                // O zaman captcha çözme servisini çaðýrarak doðrulama kodunu çöz ve tekrar dene.
                // captcha kodu nu CaptchaIlan metin alanýna yazýppost
                // ile https://www.ticaretsicil.gov.tr/view/hizlierisim/guvenlikkodudogrula.php E gönderemk gerekiyor
                //SONUÇ 1 ise doðrulama kodu doðrundý  demektirr.
                //O zaman tekrar DownloadPdfTextAsync metodunu çaðýrarak pdf içeriðini çekmeye çalýþabilirsin.

                if (htmlContent.Contains("<form id=\"FormGuvenlikKodu\">"))
                {
                    _logger.LogWarning("Sayfa doðrulama kodu gerektiriyor. Captcha çözme iþlemi baþlatýlýyor...");
                    var captchaService = new CaptchaService(this, _logger);
                    var captchaResponse = await captchaService.LoadCaptchaAsync();
                    if (captchaResponse.IsCriticalError)
                    {
                        throw new Exception($"Captcha yüklenirken kritik bir hata oluþtu: {captchaResponse.Message}");
                    }
                    if (captchaResponse.RequiresManualInput)
                    {
                        throw new Exception("Sayfa doðrulama kodu gerektiriyor ve otomatik çözüm baþarýsýz oldu. Lütfen manuel olarak doðrulama kodunu girin.");
                    }
                    // Doðrulama kodunu sunucuya gönder
                    var verifyResponse = await PostMultipartAsync("/view/hizlierisim/guvenlikkodudogrula.php", new Dictionary<string, string>
                    {
                        { "CaptchaIlan", captchaResponse.AutoResolvedText ?? string.Empty }
                    });
                    if (!verifyResponse.Content.Equals("1"))
                    {
                        throw new Exception("Doðrulama kodu doðrulanamadý. Lütfen tekrar deneyin.");
                    }
                    _logger.Log("Doðrulama kodu baþarýyla doðrulandý. PDF içeriði çekilmeye çalýþýlýyor...");
                     return await DownloadPdfAsync(pdfUrl); // Doðrulama baþarýlýysa iþlemi tekrar dene
                }

                var pdfPath = ExtractPdfUrlFromHtml(htmlContent);

                if (string.IsNullOrWhiteSpace(pdfPath))
                    throw new Exception("Sayfa içerisinde PDF dökümaný bulunamadý.");

                // 2. PDF dosyasýný indir
                var absolutePdfUrl = pdfPath.StartsWith("http") ? pdfPath : new Uri(new Uri(BaseUrl), pdfPath).ToString();
                var pdfBytes = await GetByteArrayAsync(absolutePdfUrl);

                return pdfBytes;
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Bu exception'ý üst katmana ilet
            }
            catch (Exception ex)
            {
                _logger.LogError($"PDF Ýþleme Hatasý: {ex.Message}");
                throw new Exception($"PDF Ýþleme Hatasý: {ex.Message}", ex);
            }
        }

        
       


        private bool IsSessionExpired(string htmlContent)
        {
            // Oturum dolmuþsa veya giriþ yapýlmamýþsa login sayfasýna yönlendirir
            //eðer html kodlarýnda "view/menu/cikis.php" yok ise giriþ yapýlmamýþ veya oturum dolmuþ demektir.
            //Çünkü bu link sadece giriþ yapýldýktan sonra görünür.
            return htmlContent.Contains("GÝRÝÞ YAPMALISINIZ");
                   
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

        // HttpClientWrapper içinde çerezleri kaydederken:
        public void SaveSession()
        {
            _fileLock.Wait(); // Dosya yazýmýný kilitle
            try
            {
                var uri = new Uri(BaseUrl);
                var cookieList = _cookieContainer.GetCookies(uri).Cast<Cookie>()
                    .Select(c => new CookieDto
                    {
                        Name = c.Name,
                        Value = c.Value,
                        Domain = c.Domain,
                        Path = c.Path,
                        Expires = c.Expires == DateTime.MinValue ? null : c.Expires
                    }).ToList();

                var json = JsonSerializer.Serialize(cookieList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_sessionFilePath, json);
                _logger.Log("Oturum çerezleri baþarýyla diske kaydedildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Oturum kaydedilirken hata: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public void LoadSession()
        {
            if (!File.Exists(_sessionFilePath)) return;

            _fileLock.Wait(); // Okurken dosyanýn yazýlmadýðýndan emin ol
            try
            {
                var json = File.ReadAllText(_sessionFilePath);
                var cookieDtos = JsonSerializer.Deserialize<List<CookieDto>>(json);

                if (cookieDtos != null)
                {
                    var uri = new Uri(BaseUrl);
                    foreach (var dto in cookieDtos)
                    {
                        var cookie = new Cookie(dto.Name, dto.Value, dto.Path, dto.Domain);
                        if (dto.Expires.HasValue) cookie.Expires = dto.Expires.Value;

                        _cookieContainer.Add(uri, cookie);
                    }
                    _logger.Log($"Diskten {cookieDtos.Count} adet çerez yüklendi.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Oturum yüklenirken hata: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }

    
}