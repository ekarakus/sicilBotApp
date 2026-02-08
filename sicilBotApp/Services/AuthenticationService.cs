using sicilBotApp.Infrastructure;
using sicilBotApp.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sicilBotApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly ICaptchaService _captchaService; // Eksik baðýmlýlýk eklendi
        private readonly ILogger _logger;

        private const string LoginUrl = "https://www.ticaretsicil.gov.tr/view/modal/uyegirisi_ok.php";
        private const string LoginEmail = "sicil.sorgulama@mail.com"; // Sabit bilgiler
        private const string LoginPassword = "Alo12345.";

        public bool IsAuthenticated { get; private set; }

        public AuthenticationService(IHttpClientWrapper httpClient, ICaptchaService captchaService, ILogger logger)
        {
            _httpClient = httpClient;
            _captchaService = captchaService;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> LoginAsync(string? captchaText = null)
        {
            try
            {
                // 1. Captcha saðlanmamýþsa otomatik çözmeyi dene
                if (string.IsNullOrEmpty(captchaText))
                {
                    _logger.Log("Captcha yükleniyor ve otomatik çözülmeye çalýþýlýyor...");
                    var captchaResponse = await _captchaService.LoadCaptchaAsync();

                    if (captchaResponse.IsCriticalError)
                    {
                        return new ApiResponse<bool> { Success = false, Message = captchaResponse.Message };
                    }

                    if (captchaResponse.RequiresManualInput)
                    {
                        // Program.cs bu mesajý kontrol ederek kullanýcýdan giriþ ister
                        return new ApiResponse<bool>
                        {
                            Success = false,
                            Message = "CAPTCHA_REQUIRED",
                            Data = false
                        };
                    }

                    captchaText = captchaResponse.AutoResolvedText;
                }

                // 2. Login iþlemini gerçekleþtir
                _logger.Log($"Giriþ yapýlýyor... (Captcha: {captchaText})");

                var parameters = new Dictionary<string, string>
                {
                    { "LoginEmail", LoginEmail },
                    { "LoginSifre", LoginPassword },
                    { "Captcha", captchaText! }
                };

                var response = await _httpClient.PostMultipartAsync(LoginUrl, parameters);

                // TOBB sitesi baþarýlý giriþte "1" döner
                if (response.Content?.Trim() == "1")
                {
                    IsAuthenticated = true;
                    _httpClient.SaveSession();
                    _logger.Log("Giriþ baþarýlý");
                    return new ApiResponse<bool> { Success = true, Message = "Giriþ baþarýlý", Data = true };
                }

                _logger.LogError("Giriþ baþarýsýz. Captcha veya bilgiler hatalý.");
                return new ApiResponse<bool> { Success = false, Message = "Giriþ baþarýsýz.", Data = false };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Giriþ hatasý: {ex.Message}");
                return new ApiResponse<bool> { Success = false, Message = ex.Message, Data = false };
            }
        }
   
    
    }
}