using Microsoft.Extensions.Logging;

namespace FellsideDigital.Tests.TestSupport;

/// <summary>An <see cref="ILogger"/> that records what was logged, for asserting on it.</summary>
public sealed class CapturingLogger : ILogger
{
    public readonly List<(LogLevel Level, string Message, Exception? Exception)> Entries = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception), exception));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
