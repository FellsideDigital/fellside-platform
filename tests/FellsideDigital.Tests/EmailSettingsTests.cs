using FellsideDigital.Web.Models;

namespace FellsideDigital.Tests;

public class EmailSettingsTests
{
    private static EmailSettings Complete() => new()
    {
        TenantId = "t", ClientId = "c", ClientSecret = "s",
        FromAddress = "hello@example.com", AdminEmail = "admin@example.com"
    };

    [Fact]
    public void IsConfigured_true_when_all_graph_fields_present()
        => Assert.True(Complete().IsConfigured);

    [Theory]
    [InlineData(nameof(EmailSettings.TenantId))]
    [InlineData(nameof(EmailSettings.ClientId))]
    [InlineData(nameof(EmailSettings.ClientSecret))]
    [InlineData(nameof(EmailSettings.FromAddress))]
    public void IsConfigured_false_when_any_graph_field_missing(string missing)
    {
        var s = Complete();
        typeof(EmailSettings).GetProperty(missing)!.SetValue(s, "  ");
        Assert.False(s.IsConfigured);
    }
}
