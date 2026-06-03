using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web;
using Microsoft.Extensions.Logging;

namespace FellsideDigital.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public void LogAndDescribe_returns_a_safe_message_without_exception_detail()
    {
        var logger = new CapturingLogger();
        var ex = new InvalidOperationException("secret internal detail");

        var message = ErrorHandling.LogAndDescribe(logger, ex, "saving the project");

        Assert.DoesNotContain("secret internal detail", message);
        Assert.Contains("saving the project", message);
    }

    [Fact]
    public void LogAndDescribe_logs_the_exception_at_error_level()
    {
        var logger = new CapturingLogger();
        var ex = new Exception("boom");

        ErrorHandling.LogAndDescribe(logger, ex, "doing the thing");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(ex, entry.Exception);
    }
}
