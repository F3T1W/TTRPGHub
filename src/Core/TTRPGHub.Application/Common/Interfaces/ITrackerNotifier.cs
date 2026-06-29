using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;

namespace TTRPGHub.Common.Interfaces;

public interface ITrackerNotifier
{
    Task NotifyTrackerUpdatedAsync(Guid trackerId, TrackerDetailDto state, CancellationToken ct = default);
}
