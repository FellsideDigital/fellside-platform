using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class LiveShowcaseStateTests
{
    private static LiveParticipant P(string name) => new(name, null, DateTimeOffset.UtcNow);

    [Fact]
    public void Publish_increments_count_and_raises_event()
    {
        var state = new LiveShowcaseState();
        LiveParticipant? seen = null;
        state.ParticipantJoined += p => seen = p;

        state.Publish(P("Sam"));

        Assert.Equal(1, state.Snapshot().Count);
        Assert.Equal("Sam", seen?.Name);
    }

    [Fact]
    public void Snapshot_returns_recent_newest_first_capped_at_eight()
    {
        var state = new LiveShowcaseState();
        for (var i = 0; i < 10; i++) state.Publish(P($"P{i}"));

        var snap = state.Snapshot();

        Assert.Equal(10, snap.Count);
        Assert.Equal(8, snap.Recent.Count);
        Assert.Equal("P9", snap.Recent[0].Name);
    }

    [Fact]
    public void SessionParticipants_accumulates_all_uncapped_and_reset_clears()
    {
        var state = new LiveShowcaseState();
        for (var i = 0; i < 12; i++) state.Publish(P($"P{i}"));

        var session = state.SessionParticipants();
        Assert.Equal(12, session.Count);            // not capped like Recent
        Assert.Equal("P0", session[0].Name);        // oldest-first
        Assert.Equal("P11", session[^1].Name);

        state.Reset();
        Assert.Empty(state.SessionParticipants());
    }

    [Fact]
    public void Reset_clears_state_and_raises_event()
    {
        var state = new LiveShowcaseState();
        state.Publish(P("Sam"));
        var raised = false;
        state.ResetRequested += () => raised = true;

        state.Reset();

        Assert.Equal(0, state.Snapshot().Count);
        Assert.Empty(state.Snapshot().Recent);
        Assert.True(raised);
    }
}
