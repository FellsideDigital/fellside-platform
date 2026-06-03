namespace FellsideDigital.UI.Components.Feedback;

public enum ToastLevel { Success, Error, Info, Warning }

/// <summary>A single transient notification shown by <see cref="ToastHost"/>.</summary>
public sealed record Toast
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; init; }
    public string? Title { get; init; }
    public ToastLevel Level { get; init; } = ToastLevel.Info;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(4);
}
