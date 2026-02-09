using sicilBotApp.Services;
using sicilBotApp.Infrastructure;

namespace sicilBotApp.Extensions
{
    public static class AuthenticationExtensions
    {
        public static async Task<T> ExecuteWithRetryAsync<T>(
            this IAuthenticationService authService,
            ICustomLogger logger,
            Func<Task<T>> action)
        {
            try
            {
                // 1. Adım: İşlemi normal şekilde dene
                return await action();
            }
            catch (UnauthorizedAccessException)
            {
                // 2. Adım: Eğer oturum hatası gelirse günlüğe yaz ve login ol
                logger.LogWarning("Oturumun düştüğü tespit edildi. Yeniden giriş deneniyor...");

                // AuthenticationService içindeki Semaphore sayesinde 
                // aynı anda gelen 10 istekten sadece biri login olur, diğerleri bekler.
                var loginResult = await authService.LoginAsync();

                if (loginResult.Success)
                {
                    logger.Log("Yeniden giriş başarılı. İşlem tekrarlanıyor...");
                    // 3. Adım: İşlemi son kez tekrar dene
                    return await action();
                }
                else
                {
                    logger.LogError("Yeniden giriş başarısız oldu.");
                    throw new Exception($"Oturum tazelenemedi: {loginResult.Message}");
                }
            }
        }
    }
}