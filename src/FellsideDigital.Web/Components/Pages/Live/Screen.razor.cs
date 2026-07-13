using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Live;

public partial class Screen : ComponentBase, IDisposable
{
    [Inject] private LiveShowcaseState Live { get; set; } = default!;

    private const int MaxFeed = 8;
    private readonly Queue<LiveParticipant> _queue = new();
    private readonly List<LiveParticipant> _feed = new();
    private int _count;
    private bool _running;
    private int _generation;

    private LiveParticipant? _current;
    private int _activeStage = -1;
    private bool _stageComplete;
    private string[] _stages = [];

    protected override void OnInitialized()
    {
        var snap = Live.Snapshot();
        _count = snap.Count;
        _feed.AddRange(snap.Recent);
        Live.ParticipantJoined += OnJoined;
        Live.ResetRequested += OnReset;
    }

    private void OnJoined(LiveParticipant p) => _ = InvokeAsync(async () =>
    {
        _count++;
        _feed.Insert(0, p);
        if (_feed.Count > MaxFeed) _feed.RemoveAt(_feed.Count - 1);
        _queue.Enqueue(p);
        StateHasChanged();
        await DrainAsync();
    });

    private void OnReset() => _ = InvokeAsync(() =>
    {
        _generation++;
        _count = 0;
        _feed.Clear();
        _queue.Clear();
        _current = null;
        _activeStage = -1;
        StateHasChanged();
    });

    private async Task ResetAsync()
    {
        Live.Reset(); // raises ResetRequested → OnReset marshals the UI update
        await Task.CompletedTask;
    }

    private async Task DrainAsync()
    {
        if (_running) return;
        _running = true;
        var gen = _generation;

        try
        {
            while (_queue.Count > 0)
            {
                if (gen != _generation) return; // a Reset superseded this run

                var p = _queue.Dequeue();
                _current = p;
                _stages =
                [
                    "Lead captured",
                    $"Enriching {(string.IsNullOrEmpty(p.Company) ? "profile" : p.Company)}…",
                    "CRM record created",
                    "✉ Welcome email sent",
                    "✓ Done",
                ];

                for (var i = 0; i < _stages.Length; i++)
                {
                    _activeStage = i;
                    _stageComplete = false;
                    StateHasChanged();
                    await Task.Delay(900);
                    if (gen != _generation) return;
                    _stageComplete = true;
                    StateHasChanged();
                }

                await Task.Delay(1200);
                if (gen != _generation) return;
                _current = null;
                _activeStage = -1;
                StateHasChanged();
            }
        }
        finally
        {
            _running = false;
        }
    }

    private static string StageRowClass(bool active, bool done) =>
        "flex items-center gap-4 rounded-xl px-4 py-3 border transition-all duration-300 " +
        (active || done
            ? "text-slate-900 dark:text-white border-accent bg-accent/5"
            : "text-slate-400 border-slate-100 dark:border-white/5");

    private static string StageDotClass(bool active, bool done) =>
        "w-3 h-3 rounded-full transition-colors " + (active || done ? "bg-accent" : "bg-slate-300");

    public void Dispose()
    {
        Live.ParticipantJoined -= OnJoined;
        Live.ResetRequested -= OnReset;
    }
}
