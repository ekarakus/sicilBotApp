using sicilBotApp.Infrastructure;

namespace sicilBotApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly ILogger _logger;
        private const string LoginUrl = "https://www.ticaretsicil.gov.tr/view/modal/uyegirisi_ok.php";
        private const string LoginEmail = "sicil.sorgulama@mail.com";
        private const string LoginPassword = "Alo12345.";

        public bool IsAuthenticated { get; private set; }

        public AuthenticationService(IHttpClientWrapper httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DTOs.ApiResponse<bool>> LoginAsync(string captchaText)
        {
            try
            {
                _logger.Log("Giriþ yapýlýyor...");

                var parameters = new Dictionary<string, string>
                {
                    { "LoginEmail", LoginEmail },
                    { "LoginSifre", LoginPassword },
                    { "Captcha", captchaText }
                };

                var response = await _httpClient.PostMultipartAsync(LoginUrl, parameters);

                if (response.Content?.Trim() == "1")
                {
                    IsAuthenticated = true;
                    _logger.Log("Giriþ baþarýlý");

                    return new DTOs.ApiResponse<bool>
                    {
                        Success = true,
                        Message = "Giriþ baþarýlý",
                        Data = true
                    };
                }

                _logger.LogError("Giriþ baþarýsýz");
                return new DTOs.ApiResponse<bool>
                {
                    Success = false,
                    Message = "Giriþ baþarýsýz. Captcha yanlýþ olabilir.",
                    Data = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Giriþ hatasý: {ex.Message}");
                return new DTOs.ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                };
            }
        }

        public async Task<DTOs.ApiResponse<bool>> LoginAsync(string? captchaCode = null)
        {
            try
            {
                // Eðer captcha kodu verilmemiþse, otomatik yükle
                if (string.IsNullOrEmpty(captchaCode))
                {
                    _logger.Log("Captcha yükleniyor...");
                    var captchaResponse = await _captchaService.LoadCaptchaAsync();

                    if (captchaResponse.IsCriticalError)
                    {
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = captchaResponse.Message,
                            Data = null
                        };
                    }

                    if (captchaResponse.RequiresManualInput)
                    {
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = "Manuel captcha giriþi gerekli",
                            Data = captchaResponse.CaptchaImageBase64
                        };
                    }

                    captchaCode = captchaResponse.AutoResolvedText;
                }

                // Login iþlemi
                _logger.Log($"Giriþ yapýlýyor... (Captcha: {captchaCode})");

                var parameters = new Dictionary<string, string>
                {
                    { "LoginEmail", _credentials.Email },
                    { "LoginSifre", _credentials.Password },
                    { "Captcha", captchaCode! }
                };

                var response = await _httpClient.PostMultipartAsync(LoginUrl, parameters);

                if (response.Content?.Contains("1") == true)
                {
                    IsAuthenticated = true;
                    _logger.Log("Giriþ baþarýlý!");
                    return new ApiResponse<string>
                    {
                        Success = true,
                        Message = "Giriþ baþarýlý"
                    };
                }

                _logger.LogWarning("Giriþ baþarýsýz, captcha yanlýþ olabilir");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Giriþ baþarýsýz. Captcha kodu yanlýþ olabilir.",
                    Data = Convert.ToBase64String(_captchaService.GetLastCaptchaImage() ?? Array.Empty<byte>())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Giriþ hatasý: {ex.Message}");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}