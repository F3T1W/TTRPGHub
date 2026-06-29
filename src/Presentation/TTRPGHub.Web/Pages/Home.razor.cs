namespace TTRPGHub.Pages;

public partial class Home
{
    private static readonly (string Icon, string Title, string Description)[] Features =
    [
        ("bi bi-person-badge",      "Персонажи",  "Создавай и храни своих героев с полными листами персонажа"),
        ("bi bi-dice-5",            "Сессии",     "Ищи игры или организуй собственные кампании"),
        ("bi bi-journal-bookmark",  "Правила",    "Полный справочник по D&D 5e и другим системам"),
        ("bi bi-people",            "Сообщество", "Форум, LFG и обмен опытом с другими игроками"),
    ];
}
