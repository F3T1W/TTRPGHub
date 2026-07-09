using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Common.Interfaces;

public interface IChroniclePdfService
{
    byte[] Generate(Character character, PathfinderSocietyChronicle chronicle);
}
