using FellsideDigital.Web.Services;
using FellsideDigital.Web.Services.Live;
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

    // Results dashboard state
    private bool _stopped;
    private bool _reveal;
    private LiveMetrics _metrics = LiveMetrics.Empty;

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
        _stopped = false;
        _reveal = false;
        _metrics = LiveMetrics.Empty;
        StateHasChanged();
    });

    private async Task ResetAsync()
    {
        Live.Reset(); // raises ResetRequested → OnReset marshals the UI update
        await Task.CompletedTask;
    }

    private async Task StopAsync()
    {
        _metrics = LiveMetricsBuilder.Build(Live.SessionParticipants());
        _stopped = true;
        _reveal = false;
        StateHasChanged();

        // Second pass flips _reveal so the charts animate in from zero.
        await Task.Delay(60);
        _reveal = true;
        StateHasChanged();
    }

    private void Resume()
    {
        _stopped = false;
        _reveal = false;
        StateHasChanged();
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
            if (_queue.Count > 0) _ = DrainAsync();
        }
    }

    private static string StageRowClass(bool active, bool done) =>
        "flex items-center gap-5 rounded-2xl px-6 py-5 border transition-all duration-300 " +
        (active || done
            ? "text-slate-900 dark:text-white border-accent bg-accent/5"
            : "text-slate-400 border-slate-100 dark:border-white/5");

    private static string StageDotClass(bool active, bool done) =>
        "w-4 h-4 rounded-full transition-colors " + (active || done ? "bg-accent" : "bg-slate-300");

    // ---- Results dashboard helpers ----

    private const double DonutR = 84;
    private static double DonutCircumference => 2 * Math.PI * DonutR;

    private record DonutSeg(string Label, int Count, double Percent, double Length, double Offset, string Color);

    private IReadOnlyList<DonutSeg> DonutSegments()
    {
        var total = _metrics.Total;
        if (total == 0) return [];

        var c = DonutCircumference;
        double cumulative = 0;
        var segs = new List<DonutSeg>(_metrics.Devices.Count);
        foreach (var d in _metrics.Devices)
        {
            var frac = (double)d.Count / total;
            var len = frac * c;
            segs.Add(new DonutSeg(d.Label, d.Count, frac * 100, len, -cumulative, DeviceColor(d.Label)));
            cumulative += len;
        }
        return segs;
    }

    private static string DeviceColor(string label) => label switch
    {
        DeviceDetector.IOS => "var(--color-accent)",
        DeviceDetector.Android => "#22c55e",
        DeviceDetector.Desktop => "#a855f7",
        _ => "#94a3b8",
    };

    private static int Percent(int count, int total) =>
        total == 0 ? 0 : (int)Math.Round(100.0 * count / total);

    // Invariant formatting so SVG numeric attributes never emit a comma decimal.
    private static string Fmt(double d) =>
        d.ToString("0.###", global::System.Globalization.CultureInfo.InvariantCulture);

    public void Dispose()
    {
        Live.ParticipantJoined -= OnJoined;
        Live.ResetRequested -= OnReset;
    }
}
