using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TTRPGHub.Persistence.Repositories;
using TTRPGHub.Persistence.Repositories.Forum;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Dnd5e;
using TTRPGHub.Repositories.Pf2e;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Seeding;
using TTRPGHub.Translation;

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
        services.AddScoped<IPf2eSpellRepository, Pf2eSpellRepository>();
        services.AddScoped<IPf2eMonsterRepository, Pf2eMonsterRepository>();
        services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IForumCategoryRepository, ForumCategoryRepository>();
        services.AddScoped<IForumTopicRepository, ForumTopicRepository>();
        services.AddScoped<IForumPostRepository, ForumPostRepository>();
        services.AddScoped<IHomebrewRepository, HomebrewRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IGameEventRepository, GameEventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<IUserCalendarPreferenceRepository, UserCalendarPreferenceRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<ISessionReminderLogRepository, SessionReminderLogRepository>();
        services.AddScoped<ITableMessageRepository, TableMessageRepository>();
        services.AddScoped<ITableTokenRepository, TableTokenRepository>();
        services.AddScoped<IGameSystemRepository, GameSystemRepository>();
        services.AddScoped<IRuleEntryRepository, RuleEntryRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<IContentReportRepository, ContentReportRepository>();

        services.AddHttpClient<GoogleTranslateService>();
        services.AddScoped<ITranslationService, GoogleTranslateService>();

        services.AddHttpClient<Open5eImporter>();
        services.AddScoped<Open5eImporter>();
        services.AddScoped<Pf2eImporter>();
        services.AddHttpClient<Open5eRulesImporter>();
        services.AddScoped<Open5eRulesImporter>();
        services.AddScoped<LegacyRuleMigrator>();
        services.AddScoped<Pf2eRulesSeeder>();
        services.AddScoped<GuidesSeeder>();

        return services;
    }
}
