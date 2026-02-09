namespace sicilBotApp.Services
{
    public interface ICaptchaService
    {
        /// <summary>
        /// Siteden güncel captcha görselini indirir ve otomatik çözmeyi dener.
        /// </summary>
        Task<DTOs.CaptchaResponse> LoadCaptchaAsync();

        /// <summary>
        /// Ham görsel verisini (byte array) alarak Tesseract üzerinden metne dönüþtürür.
        /// </summary>
        /// <param name="imageBytes">Görselin ham byte dizisi</param>
        /// <returns>Çözülen metin</returns>
        string ResolveCaptchaWithOcr(byte[] imageBytes);
    }
}