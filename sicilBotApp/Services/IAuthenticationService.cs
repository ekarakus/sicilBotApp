namespace sicilBotApp.Services
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Captcha kodu verilirse doðrudan login olur, verilmezse otomatik çözmeyi dener.
        /// </summary>
        Task<DTOs.ApiResponse<bool>> LoginAsync(string? captchaText = null);
        bool IsAuthenticated { get; }
    }
}