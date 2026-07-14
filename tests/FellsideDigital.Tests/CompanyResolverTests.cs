using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class CompanyResolverTests
{
    [Theory]
    [InlineData("sam@acme.com", "Acme")]
    [InlineData("sam@acme.co.uk", "Acme")]
    [InlineData("a@dept.acme.ac.uk", "Acme")]
    public void Resolve_returns_company_for_business_domains(string email, string expected)
        => Assert.Equal(expected, CompanyResolver.Resolve(email));

    [Theory]
    [InlineData("sam@gmail.com")]
    [InlineData("sam@hotmail.co.uk")]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Resolve_returns_null_for_generic_or_invalid(string email)
        => Assert.Null(CompanyResolver.Resolve(email));
}
