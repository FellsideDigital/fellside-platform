using FellsideDigital.Web.Components.Layout;

namespace FellsideDigital.Tests;

/// <summary>
/// Covers <see cref="AdminLayout.ResolveProjectId"/> — the pure URL parsing that decides when the
/// admin sidebar switches to its project-context nav. Paths are given in the leading-slash-free
/// form that <c>NavigationManager.ToBaseRelativePath</c> produces.
/// </summary>
public class AdminLayoutRouteTests
{
    private const string ProjectIdText = "11111111-1111-1111-1111-111111111111";
    private static readonly Guid ProjectId = Guid.Parse(ProjectIdText);

    [Theory]
    [InlineData("Admin/Projects/11111111-1111-1111-1111-111111111111")]
    [InlineData("Admin/Projects/11111111-1111-1111-1111-111111111111/Edit")]
    [InlineData("Admin/Projects/11111111-1111-1111-1111-111111111111/Documents")]
    [InlineData("Admin/Projects/11111111-1111-1111-1111-111111111111/Notes")]
    [InlineData("Admin/Projects/11111111-1111-1111-1111-111111111111/PortalPreview")]
    [InlineData("admin/projects/11111111-1111-1111-1111-111111111111")]   // case-insensitive
    [InlineData("/Admin/Projects/11111111-1111-1111-1111-111111111111")]  // tolerant of a leading slash
    public void Resolves_project_id_from_project_routes(string path)
    {
        Assert.Equal(ProjectId, AdminLayout.ResolveProjectId(path));
    }

    [Fact]
    public void Resolves_project_id_from_client_invoices_from_query()
    {
        var result = AdminLayout.ResolveProjectId(
            $"Admin/Clients/user-abc/Invoices?from={ProjectIdText}");

        Assert.Equal(ProjectId, result);
    }

    [Fact]
    public void Resolves_from_query_even_with_other_params_present()
    {
        var result = AdminLayout.ResolveProjectId(
            $"Admin/Clients/user-abc/Invoices?foo=bar&from={ProjectIdText}");

        Assert.Equal(ProjectId, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("Admin/Projects")]                                   // list page, no id
    [InlineData("Admin/Projects/Create")]                           // non-guid segment
    [InlineData("Admin/Invoices")]
    [InlineData("Admin/Enquiries")]
    [InlineData("Admin/Clients/user-abc/Invoices")]                 // no from param
    [InlineData("Admin/Clients/user-abc/Invoices?from=not-a-guid")]
    [InlineData($"Admin/Clients/user-abc/Settings?from={ProjectIdText}")] // from only applies to the Invoices page
    public void Returns_null_when_no_project_in_route(string path)
    {
        Assert.Null(AdminLayout.ResolveProjectId(path));
    }
}
