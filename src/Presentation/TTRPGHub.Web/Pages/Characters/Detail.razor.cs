using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Characters;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IConfiguration Config { get; set; } = default!;

    private CharacterDetailDto? _char;
    private CharacterFormModel _form = new();
    private string _pdfUrl = string.Empty;
    private bool _loading = true;
    private bool _editing;
    private bool _saving;
    private bool _uploading;
    private string? _error;
    private string? _saveError;
    private string? _avatarError;
    private InputFile? _fileInput;

    private bool _showLevelUp;
    private bool _levelingUp;
    private int _newLevel;
    private string? _levelUpError;
    private LevelUpResponse? _levelUpResult;

    private List<SelectedFeatDto> _selectedFeats = [];
    private string _featSearch = string.Empty;
    private List<RuleEntrySummaryDto> _featSearchResults = [];
    private bool _featSearching;
    private string? _featError;

    private Guid? _currentUserId;
    private bool _isPrimaryOwner;
    private string _newCoOwnerUsername = string.Empty;
    private string? _coOwnerError;

    protected override async Task OnInitializedAsync()
    {
        var apiBase = ApiBaseUrl.Resolve(Config, Nav.BaseUri);
        _pdfUrl = $"{apiBase}/api/characters/{Id}/pdf";
        try
        {
            _char = await Api.GetCharacterByIdAsync(Id);
            _form = CharacterFormModel.From(_char);
            _newLevel = _char.Level + 1;
            _selectedFeats = ParseSelectedFeats(_char.SelectedFeatsJson);
            _currentUserId = await Tokens.GetUserIdAsync();
            _isPrimaryOwner = _currentUserId.HasValue && _currentUserId.Value == _char.OwnerId;
        }
        catch { _error = "Не удалось загрузить персонажа."; }
        finally { _loading = false; }
    }

    private Task OnCharacterPf2eUpdatedAsync(CharacterDetailDto c)
    {
        _char = c;
        return Task.CompletedTask;
    }

    private async Task LevelUpAsync()
    {
        if (_char is null) return;
        _levelingUp = true;
        _levelUpError = null;
        _levelUpResult = null;
        try
        {
            var result = await Api.LevelUpCharacterAsync(Id, new LevelUpRequest(_newLevel));
            _levelUpResult = result;
            _char = await Api.GetCharacterByIdAsync(Id);
            _newLevel = _char.Level + 1;
        }
        catch
        {
            _levelUpError = "Не удалось повысить уровень.";
        }
        finally
        {
            _levelingUp = false;
        }
    }

    private void StartEdit()  { _form = CharacterFormModel.From(_char!); _editing = true;  _saveError = null; }
    private void CancelEdit() { _form = CharacterFormModel.From(_char!); _editing = false; _saveError = null; }

    private async Task SaveAsync()
    {
        _saving = true; _saveError = null;
        try
        {
            await Api.UpdateCharacterAsync(Id, _form.ToRequest(Id));
            _char = await Api.GetCharacterByIdAsync(Id);
            _form = CharacterFormModel.From(_char);
            _editing = false;
        }
        catch { _saveError = "Не удалось сохранить изменения."; }
        finally { _saving = false; }
    }

    private void SetAbility(string prop, int value) => _form = prop switch
    {
        "str" => _form with { Strength     = value },
        "dex" => _form with { Dexterity    = value },
        "con" => _form with { Constitution = value },
        "int" => _form with { Intelligence = value },
        "wis" => _form with { Wisdom       = value },
        "cha" => _form with { Charisma     = value },
        _     => _form
    };

    private void ToggleSkill(string key)
    {
        var list = _form.SkillProficiencies.ToList();
        if (!list.Remove(key)) list.Add(key);
        _form = _form with { SkillProficiencies = list };
    }

    private void ToggleSavingThrow(string key)
    {
        var list = _form.SavingThrowProficiencies.ToList();
        if (!list.Remove(key)) list.Add(key);
        _form = _form with { SavingThrowProficiencies = list };
    }

    private double HpPercent() => _form.MaxHitPoints <= 0 ? 0
        : Math.Round(Math.Clamp((double)_form.CurrentHitPoints / _form.MaxHitPoints * 100, 0, 100), 1);

    private string HpColor() => HpPercent() switch { > 60 => "#22c55e", > 30 => "#f59e0b", _ => "#ef4444" };

    private async Task TriggerFileInput()
    {
        if (_fileInput is not null)
            await Js.InvokeVoidAsync("eval", "document.querySelector('input[type=file]').click()");
    }

    private async Task UploadAvatarAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;

        _uploading = true; _avatarError = null;
        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var part = new Refit.StreamPart(ms, file.Name, file.ContentType);
            var response = await Api.UploadAvatarAsync(Id, part);
            _char = _char! with { AvatarUrl = response.Url };
        }
        catch (Exception ex) when (ex.Message.Contains("5"))
        {
            _avatarError = "Файл слишком большой (макс. 5 МБ).";
        }
        catch { _avatarError = "Ошибка загрузки. Проверьте формат файла (JPEG, PNG, WebP)."; }
        finally { _uploading = false; }
    }

    private async Task ExportAsync()
    {
        if (_char is null) return;
        var export = new ImportCharacterRequest(
            _char.Name, _char.Race, _char.Class, _char.Level, _char.IsPublic,
            _char.Background, _char.Alignment, _char.ExperiencePoints,
            _char.PersonalityTraits, _char.Ideals, _char.Bonds, _char.Flaws,
            _char.Strength, _char.Dexterity, _char.Constitution,
            _char.Intelligence, _char.Wisdom, _char.Charisma,
            _char.MaxHitPoints, _char.CurrentHitPoints, _char.TemporaryHitPoints,
            _char.ArmorClass, _char.Speed, _char.HitDice,
            [.. _char.SkillProficiencies], [.. _char.SavingThrowProficiencies],
            _char.FeaturesAndTraits, _char.Equipment);

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        var filename = $"{_char.Name.Replace(" ", "_")}.json";
        await Js.InvokeVoidAsync("downloadJson", filename, json);
    }

    private static List<SelectedFeatDto> ParseSelectedFeats(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<SelectedFeatDto>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private async Task SearchFeatsAsync()
    {
        if (string.IsNullOrWhiteSpace(_featSearch))
        {
            _featSearchResults = [];
            return;
        }

        _featSearching = true;
        try
        {
            var page = await Api.GetRuleEntriesAsync("pf2e", "feat", _featSearch, 1, 10);
            _featSearchResults = page.Items;
        }
        catch { _featSearchResults = []; }
        finally { _featSearching = false; }
    }

    private async Task AddFeatAsync(RuleEntrySummaryDto entry)
    {
        if (_char is null || _selectedFeats.Any(f => f.Slug == entry.Slug)) return;

        _selectedFeats.Add(new SelectedFeatDto(entry.Slug, entry.Title, _char.Level));
        _featSearch = string.Empty;
        _featSearchResults = [];
        await SaveFeatsAsync();
    }

    private async Task RemoveFeatAsync(string slug)
    {
        _selectedFeats.RemoveAll(f => f.Slug == slug);
        await SaveFeatsAsync();
    }

    private async Task SaveFeatsAsync()
    {
        _featError = null;
        try
        {
            var json = JsonSerializer.Serialize(_selectedFeats);
            await Api.UpdateCharacterFeatsAsync(Id, new UpdateFeatsRequest(json));
        }
        catch
        {
            _featError = "Не удалось сохранить список фитов.";
        }
    }

    private async Task AddCoOwnerAsync()
    {
        if (_char is null || string.IsNullOrWhiteSpace(_newCoOwnerUsername)) return;

        _coOwnerError = null;
        try
        {
            await Api.AddCoOwnerAsync(Id, new AddCoOwnerRequest(_newCoOwnerUsername.Trim()));
            _newCoOwnerUsername = string.Empty;
            _char = await Api.GetCharacterByIdAsync(Id);
        }
        catch
        {
            _coOwnerError = "Не удалось добавить совладельца — проверьте имя пользователя.";
        }
    }

    private async Task RemoveCoOwnerAsync(Guid userId)
    {
        try
        {
            await Api.RemoveCoOwnerAsync(Id, userId);
            _char = await Api.GetCharacterByIdAsync(Id);
        }
        catch
        {
            _coOwnerError = "Не удалось убрать совладельца.";
        }
    }

    private static int CalcMod(int score) => (int)Math.Floor((score - 10) / 2.0);

    private IEnumerable<(string, int, int, string)> AbilityRows() =>
    [
        ("СИЛ", _form.Strength,     CalcMod(_form.Strength),     "str"),
        ("ЛОВ", _form.Dexterity,    CalcMod(_form.Dexterity),    "dex"),
        ("ТЕЛ", _form.Constitution, CalcMod(_form.Constitution), "con"),
        ("ИНТ", _form.Intelligence, CalcMod(_form.Intelligence), "int"),
        ("МДР", _form.Wisdom,       CalcMod(_form.Wisdom),       "wis"),
        ("ХАР", _form.Charisma,     CalcMod(_form.Charisma),     "cha"),
    ];

    private IEnumerable<(string, string, int)> SavingThrows() =>
    [
        ("Сила",          "str", CalcMod(_form.Strength)),
        ("Ловкость",      "dex", CalcMod(_form.Dexterity)),
        ("Телосложение",  "con", CalcMod(_form.Constitution)),
        ("Интеллект",     "int", CalcMod(_form.Intelligence)),
        ("Мудрость",      "wis", CalcMod(_form.Wisdom)),
        ("Харизма",       "cha", CalcMod(_form.Charisma)),
    ];

    private IEnumerable<(string, string, int)> Skills() =>
    [
        ("Акробатика",        "acrobatics",      CalcMod(_form.Dexterity)),
        ("Уход за животными", "animal_handling",  CalcMod(_form.Wisdom)),
        ("Магия",             "arcana",           CalcMod(_form.Intelligence)),
        ("Атлетика",          "athletics",        CalcMod(_form.Strength)),
        ("Обман",             "deception",        CalcMod(_form.Charisma)),
        ("История",           "history",          CalcMod(_form.Intelligence)),
        ("Проницательность",  "insight",          CalcMod(_form.Wisdom)),
        ("Запугивание",       "intimidation",     CalcMod(_form.Charisma)),
        ("Расследование",     "investigation",    CalcMod(_form.Intelligence)),
        ("Медицина",          "medicine",         CalcMod(_form.Wisdom)),
        ("Природа",           "nature",           CalcMod(_form.Intelligence)),
        ("Внимательность",    "perception",       CalcMod(_form.Wisdom)),
        ("Выступление",       "performance",      CalcMod(_form.Charisma)),
        ("Убеждение",         "persuasion",       CalcMod(_form.Charisma)),
        ("Религия",           "religion",         CalcMod(_form.Intelligence)),
        ("Ловкость рук",      "sleight_of_hand",  CalcMod(_form.Dexterity)),
        ("Скрытность",        "stealth",          CalcMod(_form.Dexterity)),
        ("Выживание",         "survival",         CalcMod(_form.Wisdom)),
    ];

    private static readonly string[] Races =
        ["Человек","Эльф","Дварф","Полурослик","Гном","Полуэльф","Полуорк","Тифлинг","Драконорождённый","Другое"];

    private static readonly string[] Classes =
        ["Варвар","Бард","Жрец","Друид","Воин","Монах","Паладин","Следопыт","Плут","Чародей","Колдун","Волшебник"];

    private static readonly string[] Alignments =
    [
        "Законопослушный добрый","Нейтральный добрый","Хаотичный добрый",
        "Законопослушный нейтральный","Истинно нейтральный","Хаотичный нейтральный",
        "Законопослушный злой","Нейтральный злой","Хаотичный злой"
    ];

    private record CharacterFormModel
    {
        public string Name { get; set; } = "";
        public string Race { get; set; } = "";
        public string Class { get; set; } = "";
        public int Level { get; set; } = 1;
        public bool IsPublic { get; set; }
        public string? Background { get; set; }
        public string? Alignment { get; set; }
        public int ExperiencePoints { get; set; }
        public string? PersonalityTraits { get; set; }
        public string? Ideals { get; set; }
        public string? Bonds { get; set; }
        public string? Flaws { get; set; }
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Wisdom { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public int MaxHitPoints { get; set; }
        public int CurrentHitPoints { get; set; }
        public int TemporaryHitPoints { get; set; }
        public int ArmorClass { get; set; } = 10;
        public int Speed { get; set; } = 30;
        public string HitDice { get; set; } = "1d8";
        public List<string> SkillProficiencies { get; set; } = [];
        public List<string> SavingThrowProficiencies { get; set; } = [];
        public string? FeaturesAndTraits { get; set; }
        public string? Equipment { get; set; }

        public static CharacterFormModel From(CharacterDetailDto c) => new()
        {
            Name = c.Name, Race = c.Race, Class = c.Class, Level = c.Level, IsPublic = c.IsPublic,
            Background = c.Background, Alignment = c.Alignment, ExperiencePoints = c.ExperiencePoints,
            PersonalityTraits = c.PersonalityTraits, Ideals = c.Ideals, Bonds = c.Bonds, Flaws = c.Flaws,
            Strength = c.Strength, Dexterity = c.Dexterity, Constitution = c.Constitution,
            Intelligence = c.Intelligence, Wisdom = c.Wisdom, Charisma = c.Charisma,
            MaxHitPoints = c.MaxHitPoints, CurrentHitPoints = c.CurrentHitPoints,
            TemporaryHitPoints = c.TemporaryHitPoints, ArmorClass = c.ArmorClass,
            Speed = c.Speed, HitDice = c.HitDice,
            SkillProficiencies = [.. c.SkillProficiencies],
            SavingThrowProficiencies = [.. c.SavingThrowProficiencies],
            FeaturesAndTraits = c.FeaturesAndTraits, Equipment = c.Equipment
        };

        public UpdateCharacterRequest ToRequest(Guid id) => new(
            id, Name, Race, Class, Level, IsPublic,
            Background, Alignment, ExperiencePoints,
            PersonalityTraits, Ideals, Bonds, Flaws,
            Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma,
            MaxHitPoints, CurrentHitPoints, TemporaryHitPoints,
            ArmorClass, Speed, HitDice,
            SkillProficiencies, SavingThrowProficiencies,
            FeaturesAndTraits, Equipment);
    }

}
