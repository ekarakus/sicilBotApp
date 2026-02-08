using System.Drawing;

namespace sicilBotApp.Services
{
    public interface ICaptchaService
    {
        Task<DTOs.CaptchaResponse> LoadCaptchaAsync();
        string ResolveCaptchaWithOcr(Image captchaImage);
    }
}