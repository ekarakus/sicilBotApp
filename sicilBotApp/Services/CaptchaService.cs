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

        public CaptchaService(IHttpClientWrapper httpClient, Infrastructure.ICustomLogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _tesseractEngine = new Lazy<TesseractEngine>(() =>
            {
                var tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                if (!Directory.Exists(tessdataPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Tessdata klasörü bulunamadý: {tessdataPath}");
                }

                return new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
            });
        }

        public async Task<DTOs.CaptchaResponse> LoadCaptchaAsync()
        {
            try
            {
                _logger.Log("Captcha görsel yükleniyor...");

                var htmlContent = await _httpClient.GetStringAsync("https://www.ticaretsicil.gov.tr/");
                var captchaUrl = ExtractCaptchaUrl(htmlContent);

                if (string.IsNullOrEmpty(captchaUrl))
                {
                    _logger.LogError("Captcha URL'i bulunamadý - Kritik hata!");
                    return new DTOs.CaptchaResponse
                    {
                        Success = false,
                        Message = "Captcha URL'i bulunamadý. Sistem captcha yükleyemiyor.",
                        RequiresManualInput = false,
                        IsCriticalError = true
                    };
                }

                var imageBytes = await _httpClient.GetByteArrayAsync(captchaUrl);
                
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogError("Captcha görseli indirilemedi - Kritik hata!");
                    return new DTOs.CaptchaResponse
                    {
                        Success = false,
                        Message = "Captcha görseli indirilemedi.",
                        RequiresManualInput = false,
                        IsCriticalError = true
                    };
                }

                _lastCaptchaImage = imageBytes;

                using var captchaImage = ConvertToImage(imageBytes);
                var ocrText = ResolveCaptchaWithOcr(captchaImage);

                var ocrSuccess = !string.IsNullOrWhiteSpace(ocrText) && ocrText.Length >= 4;

                return new DTOs.CaptchaResponse
                {
                    Success = true,
                    CaptchaImageBase64 = Convert.ToBase64String(imageBytes),
                    AutoResolvedText = ocrSuccess ? ocrText : null,
                    RequiresManualInput = !ocrSuccess,
                    IsCriticalError = false,
                    Message = ocrSuccess 
                        ? $"OCR baþarýlý: {ocrText}" 
                        : "OCR baþarýsýz, manuel giriþ gerekli"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Captcha yükleme hatasý: {ex.Message}");
                return new DTOs.CaptchaResponse
                {
                    Success = false,
                    Message = $"Captcha yükleme hatasý: {ex.Message}",
                    RequiresManualInput = false,
                    IsCriticalError = true
                };
            }
        }

        public byte[]? GetLastCaptchaImage()
        {
            return _lastCaptchaImage;
        }

        public string ResolveCaptchaWithOcr(Image captchaImage)
        {
            try
            {
                using var bitmap = new Bitmap(captchaImage);

                using var pix = Pix.LoadFromMemory(BitmapToBytes(bitmap));
                using var page = _tesseractEngine.Value.Process(pix);

                var text = page.GetText().Trim();
                text = CleanOcrResult(text);

                _logger.Log($"OCR sonucu: '{text}'");
                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError($"OCR hatasý: {ex.Message}");
                return string.Empty;
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