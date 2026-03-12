namespace PlaceNamesBlazor.Services.Locale;

/// <summary>
/// Returns the current request locale from the URL path (e.g. "no" or "en" for /no/admin, /en/search).
/// Use for building localized links in Blazor components.
/// </summary>
public interface ICurrentLocaleService
{
    /// <summary>Returns "no" or "en" from the current request path; defaults to "no" if not present.</summary>
    string GetCurrentLocale();
}
