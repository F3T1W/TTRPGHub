using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using TTRPGHub.API.Endpoints.Auth;
using TTRPGHub.API.Endpoints.Characters;
using TTRPGHub.Application;
using TTRPGHub.Infrastructure;
using TTRPGHub.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Encoder =
            System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping);

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPersistence(builder.Configuration);

    builder.Services.AddOpenApi();

    builder.Services
        .AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Postgres")
                ?? builder.Configuration["POSTGRES_CONNECTION"]!,
            name: "postgres",
            tags: ["db"])
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis")
                ?? builder.Configuration["REDIS_CONNECTION"]!,
            name: "redis",
            tags: ["cache"]);

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        await app.Services
            .CreateScope().ServiceProvider
            .GetRequiredService<AppDbContext>()
            .Database.MigrateAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Таверна Аферистов — API";
            options.Theme = ScalarTheme.Purple;
            options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapAuthEndpoints();
    app.MapCharactersEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой при старте.");
}
finally
{
    Log.CloseAndFlush();
}
