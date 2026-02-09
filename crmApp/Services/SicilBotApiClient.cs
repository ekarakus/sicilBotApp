using System;
using System.Collections.Generic;
    using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace crmApp.Services
{
    public class SicilBotApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SicilBotApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "http://localhost:5002/api/sicil"; // PORT 5002'ye güncellendi
            _httpClient.Timeout = TimeSpan.FromMinutes(2); // OCR uzun sürebilir
        }

        public async Task<ApiResponse<CaptchaResponse>> GetCaptchaAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/captcha");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<CaptchaResponse>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new ApiResponse<CaptchaResponse> { Success = false, Message = "Deserialize hatasý" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<CaptchaResponse>
                {
                    Success = false,
                    Message = $"API hatasý: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<System.Collections.Generic.List<GazetteInfo>>> SearchAsync(
            CompanySearchRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/search", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse<System.Collections.Generic.List<GazetteInfo>>>(
                    responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new ApiResponse<System.Collections.Generic.List<GazetteInfo>> 
                    { 
                        Success = false, 
                        Message = "Deserialize hatasý" 
                    };
            }
            catch (Exception ex)
            {
                return new ApiResponse<System.Collections.Generic.List<GazetteInfo>>
                {
                    Success = false,
                    Message = $"API hatasý: {ex.Message}"
                };
            }
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // crmApp\Services\SicilBotApiClient.cs

        //bu metot gazete url li verilen pdf gazetenin metnini elde eder
        public async Task<string> GetGazetteText(string gazetteUrl)
        {
            try
            {
                //metin olarak döndürülecek gazete url'sini API'ye gönderiyoruz
                var url = $"{_baseUrl}/getGazetteText?gazetteUrl={Uri.EscapeDataString(gazetteUrl)}";
                
                


                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return $"API hatasý: {response.StatusCode}";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return apiResponse?.Data ?? string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    // DTO'lar
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class CompanySearchRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string RegisterOffice { get; set; } = string.Empty;
        public string? ManualCaptcha { get; set; }
    }

    public class CaptchaResponse
    {
        public string? CaptchaImageBase64 { get; set; }
        public string? AutoResolvedText { get; set; }
        public bool RequiresManualInput { get; set; }
        public bool IsCriticalError { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GazetteInfo
    {
        public string RegisterOffice { get; set; } = string.Empty;
        public string RegisterNumber { get; set; } = string.Empty;
        public string CompanyTitle { get; set; } = string.Empty;
        public string PublicationDate { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public string PageNumber { get; set; } = string.Empty;
        public string AnnouncementType { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty;
        public string VisitUrl { get; set; } = string.Empty;
        public string AnnouncementId { get; set; } = string.Empty;
    }
}