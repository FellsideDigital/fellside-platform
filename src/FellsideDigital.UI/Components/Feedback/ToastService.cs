namespace FellsideDigital.UI.Components.Feedback;

/// <summary>
/// Per-circuit notification queue. Inject anywhere to raise transient feedback; a single
/// <see cref="ToastHost"/> in each root layout renders and auto-dismisses the toasts.
/// </summary>
public sealed class ToastService
{
    private readonly List<Toast> _toasts = [];

    public IReadOnlyList<Toast> Toasts => _toasts;

    /// <summary>Raised whenever the toast collection changes.</summary>
    public event Action? OnChange;

    public void Success(string message, string? title = null) =>
        Add(message, title, ToastLevel.Success, TimeSpan.FromSeconds(4));

    public void Info(string message, string? title = null) =>
        Add(message, title, ToastLevel.Info, TimeSpan.FromSeconds(4));

    public void Warning(string message, string? title = null) =>
        Add(message, title, ToastLevel.Warning, TimeSpan.FromSeconds(5));

    public void Error(string message, string? title = null) =>
        Add(message, title, ToastLevel.Error, TimeSpan.FromSeconds(7));

    public void Dismiss(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
            OnChange?.Invoke();
    }

    private void Add(string message, string? title, ToastLevel level, TimeSpan duration)
    {
        _toasts.Add(new Toast
        {
            Message = message,
            Title = title,
            Level = level,
            Duration = duration,
        });
        OnChange?.Invoke();
    }
}
