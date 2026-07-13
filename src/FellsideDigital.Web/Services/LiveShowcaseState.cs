namespace FellsideDigital.Web.Services;

public record LiveParticipant(string Name, string? Company, DateTimeOffset JoinedAt);

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

    public void Reset()
    {
        lock (_lock)
        {
            _count = 0;
            _recent.Clear();
        }
        ResetRequested?.Invoke();
    }
}
