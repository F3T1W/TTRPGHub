using TTRPGHub.Entities;

namespace TTRPGHub.Common.Interfaces;

public interface ICharacterPdfService
{
    byte[] Generate(Character character, byte[]? avatarBytes = null);
}
