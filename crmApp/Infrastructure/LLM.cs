using crmApp.Infrastructure;
using Newtonsoft.Json;
    using System;
using System.Collections.Generic;
using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class OpenRouterLLMProvider : ILLMProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<ExtractionResult> ExtractDataAsync(GazetteQueryCriteria criteria)
        {
            var apiKey = AppSettings.OpenRouterApiKey;
            var model = AppSettings.OpenRouterModel;

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("API Key is missing in Web.config");

            var prompt = BuildPrompt(criteria);

            var payload = new
            {
                model = model,
                messages = new[] { new { role = "user", content = prompt } },
                response_format = new { type = "json_object" }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions"))
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"API Error: {response.StatusCode} - {responseBody}");

                return ParseResponse(responseBody);
            }
        }

    private string BuildPrompt(GazetteQueryCriteria c)
    {
        return $@"
        Rol: Ticaret Sicili Uzmanı.
        Hedef: Kaynak metin içerisinden kriterleri belirtilen şirkete ait ilan bloğunu ayıkla.

        Arama Kriterleri:
        - Şirket Unvanı: {c.CompanyName}
        - Sicil Numarası: {c.RegistrationNumber}
        - Sicil Müdürlüğü: {c.RegistryOffice}
        - İlan Bilgileri: Sayı {c.IssueNumber}, Sayfa {c.PageNumber}, Tarih {c.PublicationDate}

        Kaynak İçerik:
        ---
        {c.RawGazetteText}
        ---

        Talimatlar:
        1. İlan metni içerisinden şirketin güncel adresini ve ilanın tam metnini ayıkla.
        2. 'ilan_metni' alanını kesinlikle DEĞİŞTİRME; imla hataları dahil metni olduğu gibi (birebir) aktar.
        3. Yanıt olarak sadece ve sadece aşağıdaki JSON formatını döndür, ek açıklama yapma.

        JSON Formatı:
        {{ 
            'firma': '', 
            'adres': '', 
            'ilan_metni': '' 
        }}";
    }

    private ExtractionResult ParseResponse(string json)
    {
        dynamic data = JsonConvert.DeserializeObject(json);
        string contentJson = data.choices[0].message.content;
        var extractionResults = JsonConvert.DeserializeObject<List<ExtractionResult>>(contentJson);
        if (extractionResults == null || extractionResults.Count == 0)
        {
            throw new InvalidOperationException("LLM yanıtından herhangi bir ilan bulunamadı.");
        }

        return extractionResults[0];
    }
}
