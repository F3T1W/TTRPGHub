using TTRPGHub.Domain.Common;

namespace TTRPGHub.Domain.Entities;

public sealed class Character : Entity<CharacterId>
{
    public UserId OwnerId { get; private init; }
    public string Name { get; private set; } = null!;
    public string Race { get; private set; } = null!;
    public string Class { get; private set; } = null!;
    public int Level { get; private set; }
    public string? Notes { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private Character() { }

    public static Result<Character> Create(
        UserId ownerId,
        string name,
        string race,
        string @class,
        int level)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Имя персонажа не может быть пустым.");

        if (level is < 1 or > 20)
            return Error.Validation(nameof(Level), "Уровень должен быть от 1 до 20.");

        var now = DateTime.UtcNow;
        return new Character
        {
            Id = CharacterId.New(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Race = race,
            Class = @class,
            Level = level,
            IsPublic = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void LevelUp()
    {
        if (Level >= 20) return;
        Level++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }
}
