using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("sam@acme.com")]
    [InlineData("a.b@sub.example.co.uk")]
    public void IsValid_accepts_well_formed(string email) => Assert.True(EmailValidator.IsValid(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-at")]
    [InlineData("two@@at.com")]
    [InlineData("trailing@dot.")]
    [InlineData("space in@email.com")]
    public void IsValid_rejects_malformed(string? email) => Assert.False(EmailValidator.IsValid(email));
}
