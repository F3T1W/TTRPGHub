using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TTRPGHub.Auth;
using TTRPGHub.Caching;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Email;
using TTRPGHub.Interfaces;
using TTRPGHub.Infrastructure.Pdf;
using TTRPGHub.Push;
using TTRPGHub.Storage;

namespace TTRPGHub;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? configuration["REDIS_CONNECTION"];
        });
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IStorageService, MinioStorageService>();

        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddSingleton<ICharacterPdfService, CharacterPdfService>();

        services.Configure<VapidOptions>(configuration.GetSection("Vapid"));
        services.AddScoped<IPushNotificationSender, WebPushNotificationSender>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var secret = configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret не задан.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };

                // SignalR WebSocket-транспорт не может передать заголовок Authorization,
                // поэтому клиент шлёт токен через query-string ?access_token=...
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder();

        return services;
    }
}
