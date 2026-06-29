using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Dnd5e;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Persistence.Repositories.Forum;
using TTRPGHub.Persistence.Repositories;
using TTRPGHub.Seeding;

namespace TTRPGHub;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration["POSTGRES_CONNECTION"]
            ?? throw new InvalidOperationException("Строка подключения к БД не задана.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<IGameSessionRepository, GameSessionRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<ISessionNoteRepository, SessionNoteRepository>();
        services.AddScoped<IEncounterRepository, EncounterRepository>();
        services.AddScoped<IInitiativeTrackerRepository, InitiativeTrackerRepository>();
        services.AddScoped<IDnd5eSpellRepository, Dnd5eSpellRepository>();
        services.AddScoped<IDnd5eMonsterRepository, Dnd5eMonsterRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IForumCategoryRepository, ForumCategoryRepository>();
        services.AddScoped<IForumTopicRepository, ForumTopicRepository>();
        services.AddScoped<IForumPostRepository, ForumPostRepository>();
        services.AddScoped<IHomebrewRepository, HomebrewRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IGameEventRepository, GameEventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient<Open5eImporter>();
        services.AddScoped<Open5eImporter>();

        return services;
    }
}
