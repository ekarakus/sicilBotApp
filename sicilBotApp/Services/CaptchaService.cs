using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;
using sicilBotApp.Infrastructure;

namespace sicilBotApp.Services
{
    public class CaptchaService : ICaptchaService, IDisposable
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly ICustomLogger _logger; // ILogger yerine ICustomLogger
        private readonly Lazy<TesseractEngine> _tesseractEngine;
        private const string CaptchaPattern = @"<img[^>]*?id=['""]CaptchaImg['""][^>]*?src=['""]([^'""]+?)['""]";
        private bool _disposed;
        private byte[]? _lastCaptchaImage;
        // Tesseract thread-safe olmadýðý için kilit kullanýyoruz
        private static readonly object _ocrLock = new object();
        public CaptchaService(IHttpClientWrapper httpClient, Infrastructure.ICustomLogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _tesseractEngine = new Lazy<TesseractEngine>(() =>
            {
                var tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                return new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
            });
        }

        public async Task<DTOs.CaptchaResponse> LoadCaptchaAsync()
        {
            try
            {
                // Ana sayfayý deðil, direkt login modalýnýn olduðu yeri çekmek daha hýzlý olabilir
                // Ancak sitenin akýþýna göre ana sayfa da uygundur.
                var htmlContent = await _httpClient.GetStringAsync("https://www.ticaretsicil.gov.tr/");
                var captchaUrl = ExtractCaptchaUrl(htmlContent);

                if (string.IsNullOrEmpty(captchaUrl))
                    return ErrorResponse("Captcha URL'i bulunamadý.");

                var imageBytes = await _httpClient.GetByteArrayAsync(captchaUrl);
                if (imageBytes == null || imageBytes.Length == 0)
                    return ErrorResponse("Captcha görseli boþ döndü.");

                _lastCaptchaImage = imageBytes;

                // OCR iþlemi
                var ocrText = ResolveCaptchaWithOcr(imageBytes);

                // Ticaret Sicil captchalarý genelde 5 karakterdir. 
                // Kontrolü ona göre sýkýlaþtýrabilirsin.
                var ocrSuccess = !string.IsNullOrWhiteSpace(ocrText) && ocrText.Length >= 4;

                return new DTOs.CaptchaResponse
                {
                    Success = true,
                    CaptchaImageBase64 = Convert.ToBase64String(imageBytes),
                    AutoResolvedText = ocrSuccess ? ocrText : null,
                    RequiresManualInput = !ocrSuccess,
                    Message = ocrSuccess ? $"OCR baþarýlý: {ocrText}" : "OCR yetersiz."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Captcha hatasý: {ex.Message}");
                return ErrorResponse(ex.Message);
            }
        }
        private DTOs.CaptchaResponse ErrorResponse(string message) => new DTOs.CaptchaResponse
        {
            Success = false,
            Message = message,
            IsCriticalError = true
        };
        public byte[]? GetLastCaptchaImage()
        {
            return _lastCaptchaImage;
        }

        public string ResolveCaptchaWithOcr(byte[] imageBytes)
        {
            // Singleton olduðu için ayný anda sadece BÝR thread OCR yapabilir
            lock (_ocrLock)
            {
                try
                {
                    using var pix = Pix.LoadFromMemory(imageBytes);
                    // Görüntü iyileþtirme: Captcha genelde gürültülüdür. 
                    // Tesseract gürültülü görsellerde Gri tonlama (Grayscale) ile daha iyi çalýþýr.
                    using var processedPix = pix.ConvertRGBToGray();

                    using var page = _tesseractEngine.Value.Process(processedPix);
                    var text = page.GetText().Trim();

                    return CleanOcrResult(text);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"OCR Ýþlem Hatasý: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        private string ExtractCaptchaUrl(string html)
        {
            var match = Regex.Match(html, CaptchaPattern, RegexOptions.IgnoreCase);

            if (!match.Success) return string.Empty;

            var url = match.Groups[1].Value;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = new Uri(new Uri("https://www.ticaretsicil.gov.tr/"), url).ToString();
            }

            return url;
        }

        private static Image ConvertToImage(byte[] imageBytes)
        {
            var ms = new MemoryStream(imageBytes);
            var image = Image.FromStream(ms);
            var clonedImage = (Image)image.Clone();

            image.Dispose();
            ms.Dispose();

            return clonedImage;
        }

        private static byte[] BitmapToBytes(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static string CleanOcrResult(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return string.Empty;

            var cleaned = new string(ocrText.Where(c => char.IsLetterOrDigit(c)).ToArray());
            return cleaned;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_tesseractEngine.IsValueCreated)
                {
                    _tesseractEngine.Value?.Dispose();
                }
            }

            _disposed = true;
        }

        ~CaptchaService()
        {
            Dispose(false);
        }
    }
}