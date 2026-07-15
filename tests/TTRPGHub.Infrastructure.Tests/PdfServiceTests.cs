using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Infrastructure.Pdf;

namespace TTRPGHub.Infrastructure.Tests;

public class CharacterPdfServiceTests
{
    private readonly CharacterPdfService _service = new();

    private static Character CreateCharacter() =>
        Character.Create(UserId.New(), "Grog", "Human", "Fighter", 5).Value!;

    [Fact]
    public void Generate_ReturnsNonEmptyPdfBytes()
    {
        var pdf = _service.Generate(CreateCharacter());

        Assert.NotEmpty(pdf);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    [Fact]
    public void Generate_WithAvatarBytes_StillProducesValidPdf()
    {
        // A 1x1 transparent PNG, just enough for QuestPDF's image element to accept it.
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var pdf = _service.Generate(CreateCharacter(), pngBytes);

        Assert.NotEmpty(pdf);
    }

    [Fact]
    public void Generate_WithoutAvatar_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Generate(CreateCharacter(), null));

        Assert.Null(exception);
    }
}

public class ChroniclePdfServiceTests
{
    private readonly ChroniclePdfService _service = new();

    [Fact]
    public void Generate_ReturnsNonEmptyPdfBytes()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 5).Value!;
        var chronicle = PathfinderSocietyChronicle.Create(
            character.Id, "Scenario 1-01", DateOnly.FromDateTime(DateTime.Today),
            "GM Dave", "Grand Lodge", 12, 4, null, null).Value!;

        var pdf = _service.Generate(character, chronicle);

        Assert.NotEmpty(pdf);
        Assert.Equal((byte)'%', pdf[0]);
    }
}
