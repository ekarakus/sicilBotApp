using crmApp.Models;
using crmApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace crmApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SicilBotApiClient _apiClient;

        public HomeController(ILogger<HomeController> logger, SicilBotApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
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
        [HttpGet]
        public async Task<IActionResult> GetGazetteText(string gazetteurl)
        {
            try
            {
                var text = await _apiClient.GetGazetteText(gazetteurl);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Frontend'in beklediği formatta JSON döndür
                    return Json(new { success = true, data = text });
                }
                return Json(new { success = false, message = "Gazete metni alınamadı veya içerik boş." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGazetteText hatası");
                // Hata durumunda da frontend'e uygun formatta JSON döndür
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
