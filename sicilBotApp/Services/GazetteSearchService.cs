using sicilBotApp.DTOs;
using sicilBotApp.Extensions; // Extension'ý buraya ekledik
using sicilBotApp.Infrastructure;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sicilBotApp.Services
{
    public class GazetteSearchService : IGazetteSearchService
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IAuthenticationService _authService;
        private readonly ICustomLogger _logger;
        private const string SearchUrl = "https://www.ticaretsicil.gov.tr/view/hizlierisim/ilangoruntuleme_ok.php";

        public GazetteSearchService(
            IHttpClientWrapper httpClient,
            IAuthenticationService authService,
            ICustomLogger logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<DTOs.ApiResponse<List<DTOs.GazetteInfo>>> SearchGazettesAsync(
            DTOs.CompanySearchRequest request)
        {
            // MERKEZÝ NOKTA: Tüm arama iþlemini Retry mekanizmasýyla sarmalýyoruz.
            return await _authService.ExecuteWithRetryAsync(_logger, async () =>
            {
                _logger.Log($"Gazete aramasý yapýlýyor: {request.CompanyName}");

                var parameters = new Dictionary<string, string>
                {
                    { "SicilMudurluguId", request.RegisterOffice },
                    { "TicSicNo", request.RegisterNumber },
                    { "TicaretUnvani", request.CompanyName }
                };

                // HttpClientWrapper içindeki PostMultipartAsync veya DownloadPdfTextAsync 
                // zaten kendi içinde IsSessionExpired kontrolü yapýp UnauthorizedAccessException fýrlatýyor.
                var response = await _httpClient.PostMultipartAsync(SearchUrl, parameters);

                // Ekstra Güvenlik: Eðer sunucu yönlendirme yaptýysa bunu hata olarak fýrlat ki Retry tetiklensin
                if (response.ResponseUrl?.AbsoluteUri != SearchUrl && response.Content.Contains("login"))
                {
                    throw new UnauthorizedAccessException("Oturum geçersiz, yönlendirme saptandý.");
                }

                var gazettes = await ParseGazettes(response.Content ?? string.Empty);

                return new ApiResponse<List<GazetteInfo>>
                {
                    Success = true,
                    Message = $"{gazettes.Count} adet gazete bulundu",
                    Data = gazettes
                };
            });
        }

        // firma aramasý sonucunda gelen html içeriðinden gazete bilgilerini parse eden metot
        private async Task<List<DTOs.GazetteInfo>> ParseGazettes(string html)
        {
            var gazettes = new List<DTOs.GazetteInfo>();

            if (string.IsNullOrWhiteSpace(html))
            {
                return gazettes;
            }

            try
            {
                // Toplam gazete sayýsýný çýkar
                var countMatch = Regex.Match(html, @"Yayýnlanmýþ Türkiye Ticaret Sicili Gazetesi Ýlanlarý \((\d+) Adet\)");
                if (countMatch.Success)
                {
                    _logger.Log($"Toplam {countMatch.Groups[1].Value} adet ilan bulundu");
                }

                // Her <tr> satýrýný parse et (thead hariç, tbody içindekiler)
                var rowPattern = @"<tr>\s*<td>(?<mudurluk>.*?)</td>\s*<td[^>]*>(?<sicilNo>.*?)</td>\s*<td>(?<unvan>.*?)</td>\s*<td[^>]*>\s*(?<yayinTarihi>\d{2}\.\d{2}\.\d{4}).*?</td>\s*<td[^>]*>(?<sayi>.*?)</td>\s*<td[^>]*>(?<sayfa>.*?)</td>\s*<td[^>]*>(?<ilanTuru>.*?)</td>\s*<td[^>]*id=""IlanIdG(?<ilanId>.*?)"">\s*<a href=""(?<pdfUrl>.*?)""";

                var matches = Regex.Matches(html, rowPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var publicationDateStr = CleanHtmlText(match.Groups["yayinTarihi"].Value);
                        var gazette = new DTOs.GazetteInfo
                        {
                            RegisterOffice = CleanHtmlText(match.Groups["mudurluk"].Value),
                            RegisterNumber = CleanHtmlText(match.Groups["sicilNo"].Value),
                            CompanyTitle = CleanHtmlText(match.Groups["unvan"].Value),
                            PublicationDate = publicationDateStr,
                            PublishDate = DateTime.TryParseExact(publicationDateStr, "dd.MM.yyyy",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var date) ? date : DateTime.MinValue,
                            IssueNumber = CleanHtmlText(match.Groups["sayi"].Value),
                            PageNumber = CleanHtmlText(match.Groups["sayfa"].Value),
                            AnnouncementType = CleanHtmlText(match.Groups["ilanTuru"].Value),
                            PdfUrl = match.Groups["pdfUrl"].Value.Trim(),
                            VisitUrl = $"https://www.ticaretsicil.gov.tr/view/hizlierisim/{match.Groups["pdfUrl"].Value.Trim()}",
                            AnnouncementId = CleanHtmlText(match.Groups["ilanId"].Value)
                        };

                        gazettes.Add(gazette);
                    }
                }

                _logger.Log($"{gazettes.Count} adet gazete baþarýyla parse edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError($"HTML parse hatasý: {ex.Message}");
            }

            return gazettes;
        }
        // HTML içeriðinden gelen metni temizleyen yardýmcý metot
        private string CleanHtmlText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // HTML etiketlerini temizle
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // Fazla boþluklarý temizle
            text = Regex.Replace(text, @"\s+", " ");

            // Baþ ve sondaki boþluklarý temizle
            return text.Trim();
        }

        // gazete url'si alýr, gazeteyi indirir ve OCR ile metine dönüþtürür
        public async Task<string> GetGazetteTextAsync(string gazetteUrl)
        {
            // PDF indirme iþlemi de artýk koruma altýnda!
            return await _authService.ExecuteWithRetryAsync(_logger, async () =>
            {
                _logger.Log($"Gazete indiriliyor: {gazetteUrl}");
                return await _httpClient.DownloadPdfTextAsync(gazetteUrl);
            });
        }
        public async Task<byte[]> GetGazettePdfAsync(string pdfUrl)
        {
            // PDF indirme iþlemi de artýk koruma altýnda!
            return await _authService.ExecuteWithRetryAsync(_logger, async () =>
            {
                _logger.Log($"Gazete PDF indiriliyor: {pdfUrl}");
                return await _httpClient.DownloadPdfAsync(pdfUrl);
            });
        }
    }
}