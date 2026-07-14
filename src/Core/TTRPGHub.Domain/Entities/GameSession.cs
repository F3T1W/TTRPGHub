using TTRPGHub.Common;
using TTRPGHub.Events;

namespace TTRPGHub.Entities;

public sealed class GameSession : Entity<GameSessionId>
{
    public UserId OrganizerId { get; private init; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string System { get; private set; } = null!;   // «D&D 5e», «Pathfinder», etc.
    public int MaxPlayers { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public SessionFormat Format { get; private set; }
    public string? Location { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    // J.4 — карта/сетка/туман/стены/свет/бой раньше жили прямо здесь; вынесены в отдельную
    // сущность Scene (своя таблица и репозиторий, как TableToken), чтобы ГМ мог иметь несколько
    // карт в рамках одной игры. ActiveSceneId — какая из сцен сейчас показывается участникам;
    // сама сессия не хранит список сцен (это делает ISceneRepository.GetBySessionAsync).
    public Guid? ActiveSceneId { get; private set; }

    public string? CurrentTrackUrl { get; private set; }
    public string? CurrentTrackTitle { get; private set; }
    public bool IsAudioPlaying { get; private set; }
    public double AudioPositionSeconds { get; private set; }
    public DateTime AudioUpdatedAt { get; private set; }

    // N.6 — вариативные правила PF2e переключаются на уровне всей сессии (не сцены — это не
    // свойство конкретной карты, как туман/сетка), формулы читают флаг через TableStateDto.
    // Proficiency Without Level — бонус владения не включает уровень персонажа (Pf2eLookups.Bonus).
    public bool ProficiencyWithoutLevel { get; private set; }

    // Automatic Bonus Progression — числовые бонусы к атаке/КЗ/спасброскам/Внимательности от
    // уровня вместо рун на оружии/доспехе (Pf2eLookups.AbpBonus/ComputeArmorClass).
    public bool AutomaticBonusProgression { get; private set; }

    // Free Archetype — персонаж получает дополнительный фит архетипа на каждом чётном уровне
    // (2,4,6...), не тратя обычный классовый слот. Считается по Pf2eFeat.Source == "archetype"
    // (см. Pf2eLookups.FeatSources) — сколько фитов игрок разметил как взятые из архетипного
    // слота против Pf2eLookups.ExpectedFreeArchetypeFeats(level).
    public bool FreeArchetype { get; private set; }

    // Gradual Ability Boosts — вместо +2 к четырём характеристикам разом на 5/10/15/20 уровне
    // персонаж получает +1 на каждом уровне. Систему повышений характеристик как таковую (сами
    // значения Str/Dex/... редактируются на Character/Detail.razor вне PF2e-листа) это не меняет —
    // тоггл включает чек-лист учёта на листе персонажа (Pf2eStatsModel.AbilityBoostLevels):
    // отмечены ли уже повышения за каждый пройденный уровень, чтобы не забыть/не задвоить.
    public bool GradualAbilityBoosts { get; private set; }

    // Stamina — персонаж получает отдельный пул очков (TableToken.CurrentStamina/MaxStamina)
    // поверх обычных HP; урон в бою бьёт сначала по нему, HP остаются нетронутыми, пока Stamina
    // не кончится (см. Table.razor.cs ApplyDamageAsync/AdjustStaminaAsync). Упрощение против
    // полного правила Gamemastery Guide: не считаем Resolve Points отдельно и не завязываем
    // восстановление Stamina на конкретный вид отдыха — GM правит числа вручную, как и с HP.
    public bool StaminaVariant { get; private set; }

    // N.12 — таблица случайных встреч ГМа: одна таблица на сессию (не библиотека таблиц —
    // для одного стола обычно актуальна одна таблица под текущую локацию/акт), хранится как
    // есть в JSON {title, entries:[{min,max,label,monsterId?}]}. Редактируется на клиенте
    // (Pf2eLookups), но сам бросок (RollEncounterTableCommand) читает и разбирает этот JSON
    // на сервере — иначе результат броска можно было бы подделать с клиента.
    public string? EncounterTableJson { get; private set; }

    // R.1 — расшаренные макросы стола: GM отмечает макросы из своей личной библиотеки (K.7) как
    // видимые/запускаемые всем участникам сессии, не только себе. Храним только ссылки на чужие
    // (точнее, GM-овские) Macro.Id — сам макрос остаётся собственностью GM, ничего не копируется
    // и не дублируется; отозвать доступ = просто убрать Id из списка, не трогая исходный макрос.
    public List<Guid> SharedMacroIds { get; private set; } = [];

    private readonly List<SessionParticipant> _participants = [];
    public IReadOnlyList<SessionParticipant> Participants => _participants.AsReadOnly();

    private GameSession() { }

    public static GameSession Create(
        UserId organizerId, string title, string? description,
        string system, int maxPlayers, DateTime scheduledAt,
        SessionFormat format, string? location)
    {
        var now = DateTime.UtcNow;
        var session = new GameSession
        {
            Id = GameSessionId.New(),
            OrganizerId = organizerId,
            Title = title,
            Description = description,
            System = system,
            MaxPlayers = maxPlayers,
            ScheduledAt = scheduledAt,
            Format = format,
            Location = location,
            Status = SessionStatus.Planned,
            CreatedAt = now,
            UpdatedAt = now
        };
        session._participants.Add(SessionParticipant.Create(organizerId, session.Id, ParticipantRole.DungeonMaster));
        session.RaiseDomainEvent(new GameSessionCreatedEvent(session.Id, organizerId));
        return session;
    }

    public Error? Join(UserId userId)
    {
        if (Status != SessionStatus.Planned)
            return Error.Validation("Session.NotOpen", "Сессия не принимает новых участников.");
        if (_participants.Any(p => p.UserId == userId))
            return Error.Validation("Session.AlreadyJoined", "Вы уже участвуете в этой сессии.");
        if (_participants.Count(p => p.Role == ParticipantRole.Player) >= MaxPlayers - 1)
            return Error.Validation("Session.Full", "Мест в сессии нет.");

        _participants.Add(SessionParticipant.Create(userId, Id, ParticipantRole.Player));
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Leave(UserId userId)
    {
        if (OrganizerId == userId)
            return Error.Validation("Session.OrganizerCannotLeave", "Организатор не может покинуть сессию.");
        var p = _participants.FirstOrDefault(x => x.UserId == userId);
        if (p is null)
            return Error.Validation("Session.NotParticipant", "Вы не являетесь участником.");

        _participants.Remove(p);
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Start(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status != SessionStatus.Planned)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя начать в текущем статусе.");

        Status = SessionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Complete(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status != SessionStatus.InProgress)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя завершить в текущем статусе.");

        Status = SessionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Cancel(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status == SessionStatus.Completed || Status == SessionStatus.Cancelled)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя отменить в текущем статусе.");

        Status = SessionStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public void Update(
        string title, string? description, string system, int maxPlayers, DateTime scheduledAt,
        SessionFormat format, string? location)
    {
        Title = title;
        Description = description;
        System = system;
        MaxPlayers = maxPlayers;
        ScheduledAt = scheduledAt;
        Format = format;
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }

    // Проверка "sceneId принадлежит этой сессии" делает обработчик команды (загружает Scene и
    // сверяет Scene.SessionId == session.Id) — сущность здесь отвечает только за авторизацию ГМ.
    public Error? SetActiveScene(UserId requesterId, Guid sceneId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        ActiveSceneId = sceneId;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SetVariantRules(
        UserId requesterId, bool proficiencyWithoutLevel, bool automaticBonusProgression,
        bool freeArchetype, bool gradualAbilityBoosts, bool staminaVariant)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        ProficiencyWithoutLevel = proficiencyWithoutLevel;
        AutomaticBonusProgression = automaticBonusProgression;
        FreeArchetype = freeArchetype;
        GradualAbilityBoosts = gradualAbilityBoosts;
        StaminaVariant = staminaVariant;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SetEncounterTable(UserId requesterId, string? encounterTableJson)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        EncounterTableJson = encounterTableJson;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SetTrack(UserId requesterId, string trackUrl, string? trackTitle)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        CurrentTrackUrl = trackUrl;
        CurrentTrackTitle = trackTitle;
        IsAudioPlaying = false;
        AudioPositionSeconds = 0;
        AudioUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? PlayAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (CurrentTrackUrl is null)
            return Error.Validation("Audio.NoTrack", "Трек не выбран.");

        IsAudioPlaying = true;
        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? PauseAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        IsAudioPlaying = false;
        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SeekAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? ClearAudio(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        CurrentTrackUrl = null;
        CurrentTrackTitle = null;
        IsAudioPlaying = false;
        AudioPositionSeconds = 0;
        AudioUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public bool IsParticipant(UserId userId) => _participants.Any(p => p.UserId == userId);

    public void ShareMacro(Guid macroId)
    {
        if (!SharedMacroIds.Contains(macroId))
            SharedMacroIds.Add(macroId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnshareMacro(Guid macroId)
    {
        SharedMacroIds.Remove(macroId);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SessionStatus { Planned, InProgress, Completed, Cancelled }
public enum SessionFormat { Online, Offline, Hybrid }
