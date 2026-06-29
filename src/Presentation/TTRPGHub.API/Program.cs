using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using TTRPGHub;
using TTRPGHub.Endpoints.Auth;
using TTRPGHub.Endpoints.Characters;
using TTRPGHub.Endpoints.Sessions;
using TTRPGHub.Endpoints.Campaigns;
using TTRPGHub.Endpoints.SessionNotes;
using TTRPGHub.Endpoints.Encounters;
using TTRPGHub.Endpoints.Initiative;
using TTRPGHub.Endpoints.Dnd5e;
using TTRPGHub.Endpoints.Users;
using TTRPGHub.Hubs;
using TTRPGHub.Services;
using TTRPGHub.Seeding;
using TTRPGHub.Common.Interfaces;

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
    {
        options.SerializerOptions.Encoder =
            System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

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

    builder.Services.AddSignalR();
    builder.Services.AddScoped<ITrackerNotifier, SignalRTrackerNotifier>();

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<Open5eImporter>().ImportIfEmptyAsync();
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
    app.MapSessionsEndpoints();
    app.MapCampaigns();
    app.MapSessionNotes();
    app.MapEncounters();
    app.MapInitiative();
    app.MapDnd5e();
    app.MapUsers();
    app.MapHub<InitiativeHub>("/hubs/initiative");

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
