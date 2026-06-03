using Microsoft.Extensions.Logging;

namespace FellsideDigital.Web;

/// <summary>
/// Centralises the "log the real exception, show the user something safe" convention so no
/// catch block leaks <see cref="Exception.Message"/> into the UI.
/// </summary>
public static class ErrorHandling
{
    /// <summary>
    /// Logs <paramref name="ex"/> against <paramref name="action"/> and returns a friendly,
    /// non-leaking message suitable for display. <paramref name="action"/> should read as a
    /// gerund phrase, e.g. "saving the project".
    /// </summary>
    public static string LogAndDescribe(ILogger logger, Exception ex, string action)
    {
        logger.LogError(ex, "{Action} failed", action);
        return $"Something went wrong while {action}. Please try again.";
    }
}
