using Microsoft.AspNetCore.Mvc;
using sicilBotApp.DTOs;
using sicilBotApp.Services;
using sicilBotApp.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sicilBotApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SicilController : ControllerBase
    {
        private readonly ICaptchaService _captchaService;
        private readonly IAuthenticationService _authService;
        private readonly IGazetteSearchService _gazetteService;
        private readonly ICustomLogger _logger;

        public SicilController(
            ICaptchaService captchaService,
            IAuthenticationService authService,
            IGazetteSearchService gazetteService,
            ICustomLogger logger)
        {
            _captchaService = captchaService;
            _authService = authService;
            _gazetteService = gazetteService;
            _logger = logger;
        }

        /// <summary>
        /// Captcha görselini yükler
        /// GET /api/sicil/captcha
        /// </summary>
        [HttpGet("captcha")]
        [ProducesResponseType(typeof(ApiResponse<CaptchaResponse>), 200)]
        public async Task<IActionResult> GetCaptcha()
        {
            _logger.Log("Captcha istendi");
            var captchaResponse = await _captchaService.LoadCaptchaAsync();

            return Ok(new ApiResponse<CaptchaResponse>
            {
                Success = !captchaResponse.IsCriticalError,
                Message = captchaResponse.Message,
                Data = captchaResponse
            });
        }

        /// <summary>
        /// Gazete aramasý yapar
        /// POST /api/sicil/search
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(ApiResponse<List<GazetteInfo>>), 200)]
        public async Task<IActionResult> Search([FromBody] CompanySearchRequest request)
        {
            _logger.Log($"Arama istendi: {request?.CompanyName}");

            if (request == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Geçersiz istek"
                });
            }

            var searchResult = await _gazetteService.SearchGazettesAsync(request);
            return Ok(searchResult);
        }

        /// <summary>
        /// Health check
        /// GET /api/sicil/ping
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { Status = "Healthy", Timestamp = System.DateTime.UtcNow });
        }

        //buraya yeni bir metot ekle ve parametre olarak gelen gazete url sini alý, gazeteyi indiren ve ocr ile metine döndüdürp veren endopint

        /// <summary>
        /// burada parametre olarak bir gazete url'si alýr, gazeteyi indirir ve OCR ile metine dönüþtürür
        /// </summary>
        [HttpGet("getGazetteText")]
        public async Task<IActionResult> GetGazetteText([FromQuery] string gazetteUrl)
        {
            _logger.Log($"Gazete metni istendi: {gazetteUrl}");

            if (string.IsNullOrEmpty(gazetteUrl))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Geçersiz gazete URL'si"
                });
            }

            var ocrResult = await _gazetteService.GetGazetteTextAsync(gazetteUrl);
            return Ok(new ApiResponse<string>
            {
                Success = !string.IsNullOrEmpty(ocrResult),
                Message = string.IsNullOrEmpty(ocrResult) ? "OCR iþlemi baþarýsýz oldu" : "OCR iþlemi baþarýlý",
                Data = ocrResult
            });
        }
    }
}