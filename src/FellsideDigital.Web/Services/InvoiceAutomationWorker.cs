namespace FellsideDigital.Web.Services;

/// <summary>
/// Runs invoice generation and reminder processing in-process — once shortly after
/// startup (to catch up after downtime), then daily at a fixed morning hour. Both
/// underlying services are idempotent per day, so overlapping runs after restarts
/// are harmless. Deliberately in-process (no external scheduler/queue) so the
/// feature adds zero infrastructure cost.
/// </summary>
public class InvoiceAutomationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceAutomationWorker> logger) : BackgroundService
{
    /// <summary>09:00 UTC — a friendly mid-morning send time for UK clients year-round.</summary>
    private const int RunHourUtc = 9;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Let migrations and startup tasks settle before the first pass.
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(DelayUntilNextRun(DateTime.UtcNow), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var now = DateTime.UtcNow;

            var issued = await scope.ServiceProvider
                .GetRequiredService<IRecurringInvoiceService>()
                .GenerateDueInvoicesAsync(now);

            var reminded = await scope.ServiceProvider
                .GetRequiredService<IInvoiceReminderService>()
                .ProcessRemindersAsync(now);

            if (issued > 0 || reminded > 0)
                logger.LogInformation(
                    "Invoice automation run complete: {Issued} invoice(s) issued, {Reminded} reminder(s) sent",
                    issued, reminded);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Never let a failed pass kill the worker — try again at the next run.
            logger.LogError(ex, "Invoice automation run failed");
        }
    }

    internal static TimeSpan DelayUntilNextRun(DateTime utcNow)
    {
        var next = utcNow.Date.AddHours(RunHourUtc);
        if (next <= utcNow) next = next.AddDays(1);
        return next - utcNow;
    }
}
