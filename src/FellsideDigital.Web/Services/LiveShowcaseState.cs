namespace FellsideDigital.Web.Services;

public record LiveParticipant(
    string Name,
    string? Company,
    DateTimeOffset JoinedAt,
    string DeviceType = "Other",
    string? Domain = null);

public record LiveSnapshot(int Count, IReadOnlyList<LiveParticipant> Recent);

/// <summary>
/// In-memory, process-wide broadcaster for the live automation showcase. Phone joins
/// publish participants; the admin big screen subscribes. Count is intentionally
/// ephemeral (resets on restart or via <see cref="Reset"/>); persisted leads live in
/// the database via QrLeadService.
/// </summary>
public sealed class LiveShowcaseState
{
    private const int MaxRecent = 8;
    private readonly object _lock = new();
    private readonly List<LiveParticipant> _recent = new();
    private readonly List<LiveParticipant> _session = new();
    private int _count;

    public event Action<LiveParticipant>? ParticipantJoined;
    public event Action? ResetRequested;

    public void Publish(LiveParticipant p)
    {
        lock (_lock)
        {
            _count++;
            _recent.Insert(0, p);
            if (_recent.Count > MaxRecent) _recent.RemoveAt(_recent.Count - 1);
            _session.Add(p);
        }
        ParticipantJoined?.Invoke(p);
    }

    public LiveSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new LiveSnapshot(_count, _recent.ToList());
        }
    }

    /// <summary>
    /// Every participant since the last reset, oldest-first — the basis for the
    /// end-of-session results dashboard. Unbounded (an event is hundreds, not
    /// millions); cleared by <see cref="Reset"/>.
    /// </summary>
    public IReadOnlyList<LiveParticipant> SessionParticipants()
    {
        lock (_lock)
        {
            return _session.ToList();
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _count = 0;
            _recent.Clear();
            _session.Clear();
        }
        ResetRequested?.Invoke();
    }
}
