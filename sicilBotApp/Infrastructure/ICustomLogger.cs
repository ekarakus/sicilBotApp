namespace sicilBotApp.Infrastructure
{
    public interface ICustomLogger
    {
        void Log(string message);
        void LogError(string message);
        void LogWarning(string message);
    }
}