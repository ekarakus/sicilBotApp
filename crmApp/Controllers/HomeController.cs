using crmApp.Models;
using crmApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace crmApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SicilBotApiClient _apiClient;
        private readonly ExtractionAppService _extractionService;
        public HomeController(ILogger<HomeController> logger, SicilBotApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
            var provider = new OpenRouterLLMProvider();
            _extractionService = new ExtractionAppService(provider);
        }

        public async Task<IActionResult> Index()
        {
            var isHealthy = await _apiClient.HealthCheckAsync();
            ViewBag.ApiStatus = isHealthy ? "✓ API Çalışıyor" : "✗ API Çalışmıyor";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(sicilAramaModel model)
        {
            try
            {
                var request = new CompanySearchRequest
                {
                    CompanyName = model.TicaretUnvani ?? string.Empty,
                    RegisterNumber = model.TicSicNo ?? string.Empty,
                    RegisterOffice = model.SicilMudurluguId ?? string.Empty,
                  //  ManualCaptcha = model.CaptchaKodu
                };

                var result = await _apiClient.SearchAsync(request);

                if (result.Success && result.Data != null)
                {
                    ViewBag.Results = result.Data;
                    ViewBag.Message = result.Message;
                }
                else
                {
                    ViewBag.Error = result.Message;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sicil sorgulama hatası");
                ViewBag.Error = $"Hata: {ex.Message}";
                return View(model);
            }
        }
        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> GetGazetteText(string gazetteurl, GazetteQueryCriteria firma)
        {
            try
            {
                var text = await _apiClient.GetGazetteText(gazetteurl);


                if (string.IsNullOrWhiteSpace(text))
                {
                    // Frontend'in beklediği formatta JSON döndür
                    return Json(new { success = false, message = "Gazete metni alınamadı veya içerik boş." });

                }


                firma.RawGazetteText = text;
                var result = await _extractionService.ExecuteExtractionAsync(firma);

                if (null==result)
                {
                    // Ayıklama sonucunda metin bulunamadıysa uygun formatta JSON döndür
                    return Json(new { success = false, message = "Ayıklama sonucunda metin bulunamadı." });
                }

                return Json(new { success = true, data = result

                 });

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGazetteText hatası");
                // Hata durumunda da frontend'e uygun formatta JSON döndür
                return Json(new { success = false, message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // <summary>
        // pdf dosyasının içeriğini talep et. gelen pdf yi binary olarak alıp pdf  döndürür.
        //</summary>


        [HttpGet]
        public async Task<IActionResult> GetGazettePdf(string pdfUrl)
        {
            try
            {
                var pdfBytes = await _apiClient.GetPdfContentAsync(pdfUrl);
                if (pdfBytes != null && pdfBytes.Length > 0)
                {
                    return File(pdfBytes, "application/pdf", "document.pdf");
                }
                return NotFound("PDF içeriği alınamadı veya dosya boş.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPdfContent hatası");
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        //<summary>
        // ayıklanan pdf metninden firma bilgilerini ve gazete bilgileri kullanarak firmanın ilgili gazete kayıt metnini elde et (ayıkla)
        //  </summary>


        [HttpPost]
        public async Task<IActionResult> GetGazetteExtract(AnnouncementSearchCriteria firma,string gazeteMetni)
        {
            try
            {
                var extractedText = new GazetteExtractionService().ExtractSpecificAnnouncement(gazeteMetni, firma);
                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    return Json(new { success = true, data = extractedText });
                }
                return Json(new { success = false, message = "Eşleşen kayıt bulunamadı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gazette ayıklama hatası");
                return Json(new { success = false, message = $"Sunucu hatası: {ex.Message}" });
            }

        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
