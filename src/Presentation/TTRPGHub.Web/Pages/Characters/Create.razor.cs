using System.ComponentModel.DataAnnotations;

namespace TTRPGHub.Pages.Characters;

public partial class Create
{
    private readonly CharacterModel _model = new();
    private string? _error;
    private bool _loading;

    private static readonly string[] Races =
        ["Человек", "Эльф", "Дварф", "Полурослик", "Гном", "Полуэльф",
         "Полуорк", "Тифлинг", "Драконорождённый", "Другое"];

    private static readonly string[] Classes =
        ["Варвар", "Бард", "Жрец", "Друид", "Воин", "Монах",
         "Паладин", "Следопыт", "Плут", "Чародей", "Колдун", "Волшебник"];

    private async Task SubmitAsync()
    {
        _loading = true;
        _error   = null;

        try
        {
            var result = await Api.CreateCharacterAsync(
                new(_model.Name, _model.Race, _model.Class, _model.Level));
            Nav.NavigateTo($"/characters/{result.CharacterId}");
        }
        catch
        {
            _error = "Не удалось создать персонажа. Попробуй ещё раз.";
        }
        finally
        {
            _loading = false;
        }
    }

    private sealed class CharacterModel
    {
        [Required(ErrorMessage = "Введи имя персонажа.")]
        [StringLength(64, ErrorMessage = "Максимум 64 символа.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выбери расу.")]
        public string Race { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выбери класс.")]
        public string Class { get; set; } = string.Empty;

        [Range(1, 20)]
        public int Level { get; set; } = 1;
    }
}
