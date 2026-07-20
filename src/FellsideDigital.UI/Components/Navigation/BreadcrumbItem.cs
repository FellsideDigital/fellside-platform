namespace FellsideDigital.UI.Components.Navigation;

/// <summary>
/// A single breadcrumb entry. Give it an <see cref="Href"/> to make it a link; the last
/// item in a trail is rendered as the current page regardless of whether it has an href.
/// </summary>
public sealed record BreadcrumbItem(string Label, string? Href = null);
