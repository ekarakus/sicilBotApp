using System;
using Microsoft.Extensions.Configuration;

namespace crmApp.Infrastructure
{
    public static class AppSettings
    {
            private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        public static string OpenRouterApiKey => GetValue("OpenRouter:ApiKey");
        public static string OpenRouterModel => GetValue("OpenRouter:Model");

        private static string GetValue(string key)
        {
            var value = Configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Yapılandırmada '{key}' anahtarı bulunamadı.");
            }

            return value;
        }
    }
}
