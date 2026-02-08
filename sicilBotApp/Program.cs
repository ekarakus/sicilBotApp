using System.Text.Json;
using sicilBotApp.DTOs;
using sicilBotApp.Services;
using sicilBotApp.Infrastructure;

namespace sicilBotApp
{
    internal class Program
    {
        private static ILogger? _logger;
        private static ICaptchaService? _captchaService;
        private static IAuthenticationService? _authService;
        private static IGazetteSearchService? _gazetteService;

        static async Task<int> Main(string[] args)
        {
            try
            {
               InitializeServices();

                Console.WriteLine("╔═══════════════════════════════════════════════╗");
                Console.WriteLine("║   TİCARET SİCİL GAZETESİ SORGULAMA SERVİSİ   ║");
                Console.WriteLine("╚═══════════════════════════════════════════════╝");
                Console.WriteLine();

                // Komut satırı argümanlarını parse et
                if (args.Length > 0 && args[0] == "--api")
                {
                    return await RunAsApiMode(args);
                }

                // Interactive mode
                return await RunInteractiveMode();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Kritik Hata: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void InitializeServices()
        {
            _logger = new ConsoleLogger();
            var httpClient = new HttpClientWrapper();
            
            _captchaService = new CaptchaService(httpClient, _logger);
            _authService = new AuthenticationService(httpClient, _logger);
            _gazetteService = new GazetteSearchService(httpClient, _authService, _logger);
        }

        private static async Task<int> RunAsApiMode(string[] args)
        {
            // JSON input bekleniyor
            // Örnek kullanım: sicilBotApp.exe --api "{\"CompanyName\":\"...\",\"RegisterNumber\":\"...\"}"
            
            if (args.Length < 2)
            {
                Console.WriteLine("Kullanım: sicilBotApp.exe --api <JSON>");
                return 1;
            }

            var jsonInput = args[1];
            var request = JsonSerializer.Deserialize<CompanySearchRequest>(jsonInput);

            if (request == null)
            {
                return OutputJson(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Geçersiz JSON formatı"
                });
            }

            // Captcha işlemi
            var captchaResponse = await _captchaService!.LoadCaptchaAsync();
            
            if (captchaResponse.RequiresManualInput && string.IsNullOrEmpty(request.ManualCaptcha))
            {
                return OutputJson(new ApiResponse<CaptchaResponse>
                {
                    Success = false,
                    Message = "Manuel captcha girişi gerekli",
                    Data = captchaResponse
                });
            }

            var captchaText = !string.IsNullOrEmpty(request.ManualCaptcha) 
                ? request.ManualCaptcha 
                : captchaResponse.AutoResolvedText;

            // Login
            var loginResult = await _authService!.LoginAsync(captchaText!);
            
            if (!loginResult.Success)
            {
                return OutputJson(loginResult);
            }

            // Arama
            var searchResult = await _gazetteService!.SearchGazettesAsync(request);
            
            return OutputJson(searchResult);
        }

        private static async Task<int> RunInteractiveMode()
        {
            while (true) // Retry döngüsü
    {
        // 1. Captcha yükle
        _logger!.Log("Captcha yükleniyor...");
        var captchaResponse = await _captchaService!.LoadCaptchaAsync();

        if (captchaResponse.IsCriticalError)
        {
            _logger.LogError($"Kritik hata: {captchaResponse.Message}");
            return 1;
        }

        string captchaText;

        if (captchaResponse.RequiresManualInput)
        {
            _logger.LogWarning("OCR captcha'yı çözemedi, manuel giriş gerekli");
            Console.WriteLine($"\nCaptcha Base64: {captchaResponse.CaptchaImageBase64}");
            Console.WriteLine("(Bu Base64 string'i bir görsel olarak gösterebilirsiniz)");
            Console.Write("\nCaptcha kodunu giriniz: ");
            captchaText = Console.ReadLine() ?? string.Empty;
        }
        else
        {
            captchaText = captchaResponse.AutoResolvedText!;
            _logger.Log($"Captcha otomatik çözüldü: {captchaText}");
        }

        // 2. Login
        var loginResult = await _authService!.LoginAsync(captchaText);

        if (loginResult.Success)
        {
            break; // Giriş başarılı, döngüden çık
        }

        // Giriş başarısız
        _logger!.LogError(loginResult.Message);

        if (!string.IsNullOrEmpty(loginResult.Data))
        {
            Console.WriteLine($"\nYeni Captcha Base64: {loginResult.Data}");
            Console.Write("Captcha kodunu tekrar giriniz (veya 'q' ile çıkış): ");
            var retryCaptcha = Console.ReadLine() ?? string.Empty;

            if (retryCaptcha.ToLower() == "q")
            {
                return 1;
            }

            var retryLogin = await _authService.LoginAsync(retryCaptcha);
            if (retryLogin.Success)
            {
                break;
            }
        }

        Console.Write("\nTekrar denemek ister misiniz? (E/H): ");
        if (Console.ReadLine()?.ToUpper() != "E")
        {
            return 1;
        }
    }

    // 3. Arama parametreleri al
    Console.Write("\nFirma Adı: ");
    var companyName = Console.ReadLine() ?? string.Empty;

    Console.Write("Sicil No: ");
    var registerNo = Console.ReadLine() ?? string.Empty;

    Console.Write("Sicil Ofisi Kodu: ");
    var registerOffice = Console.ReadLine() ?? string.Empty;

    var searchRequest = new CompanySearchRequest
    {
        CompanyName = companyName,
        RegisterNumber = registerNo,
        RegisterOffice = registerOffice
    };

    // 4. Arama yap
    var searchResult = await _gazetteService!.SearchGazettesAsync(searchRequest);

    // 5. Sonuçları göster
    Console.WriteLine("\n" + new string('=', 50));
    Console.WriteLine($"Sonuç: {searchResult.Message}");
    Console.WriteLine(new string('=', 50));

    if (searchResult.Success && searchResult.Data != null)
    {
        foreach (var gazette in searchResult.Data)
        {
            Console.WriteLine($"\n {gazette.CompanyTitle}");
            Console.WriteLine($"   Tarih: {gazette.PublishDate:dd.MM.yyyy}");
            Console.WriteLine($"   İlan türü: {gazette.AnnouncementType} ");
            Console.WriteLine($"   Gazete adresi: {gazette.VisitUrl} ");
        }
    }

    return searchResult.Success ? 0 : 1;
}

        private static int OutputJson<T>(ApiResponse<T> response)
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Console.WriteLine(json);
            return response.Success ? 0 : 1;
        }
    }
}
