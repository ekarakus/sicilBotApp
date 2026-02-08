using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sicilBotApp.Services;
using sicilBotApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CrmAppPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:*")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// sicilBot servisleri - SCOPED olarak (her istek için yeni instance)
builder.Services.AddScoped<ICustomLogger, ConsoleLogger>();
builder.Services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();
builder.Services.AddScoped<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IGazetteSearchService, GazetteSearchService>();

// Health check
builder.Services.AddHealthChecks();

// Kestrel ayarları - PORT 5002'ye değiştirildi
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5002); // Farklı port kullan
});

var app = builder.Build();

// Swagger (her zaman açık test için)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("CrmAppPolicy");
// HTTPS redirect KALDIRILDI - Sadece HTTP kullanıyoruz
// app.UseHttpsRedirection(); 

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Console.WriteLine("sicilBotApp API çalışıyor: http://localhost:5002");
Console.WriteLine("Swagger UI: http://localhost:5002/swagger");

app.Run();
