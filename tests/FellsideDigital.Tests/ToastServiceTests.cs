using FellsideDigital.UI.Components.Feedback;

namespace FellsideDigital.Tests;

public class ToastServiceTests
{
    [Fact]
    public void Success_adds_toast_and_raises_event()
    {
        var sut = new ToastService();
        var raised = 0;
        sut.OnChange += () => raised++;

        sut.Success("Saved", "Done");

        var toast = Assert.Single(sut.Toasts);
        Assert.Equal("Saved", toast.Message);
        Assert.Equal("Done", toast.Title);
        Assert.Equal(ToastLevel.Success, toast.Level);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Error_uses_error_level_and_a_longer_duration()
    {
        var sut = new ToastService();

        sut.Error("Boom");

        var toast = Assert.Single(sut.Toasts);
        Assert.Equal(ToastLevel.Error, toast.Level);
        Assert.True(toast.Duration >= TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void Dismiss_removes_the_toast_and_raises_event()
    {
        var sut = new ToastService();
        sut.Info("hi");
        var id = sut.Toasts[0].Id;
        var raised = 0;
        sut.OnChange += () => raised++;

        sut.Dismiss(id);

        Assert.Empty(sut.Toasts);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Dismiss_with_unknown_id_is_a_no_op()
    {
        var sut = new ToastService();
        sut.Info("hi");
        var raised = 0;
        sut.OnChange += () => raised++;

        sut.Dismiss(Guid.NewGuid());

        Assert.Single(sut.Toasts);
        Assert.Equal(0, raised);
    }
}
