namespace sicilBotApp.Services
{
    public interface IAuthenticationService
    {
        Task<DTOs.ApiResponse<bool>> LoginAsync(string captchaText);
     //   Task<DTOs.ApiResponse<bool>> LoginWithConfigAsync();
        Task<DTOs.ApiResponse<bool>> LoginAsync();
        bool IsAuthenticated { get; }
    }
}