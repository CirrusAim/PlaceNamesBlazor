using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace PlaceNamesBlazor.Services.Locale;

/// <summary>
/// Returns the current locale from the URL. Uses NavigationManager (current browser URL) when available
/// so locale is correct after client-side navigation; falls back to HttpContext for initial request or Razor Pages.
/// </summary>
public class CurrentLocaleService : ICurrentLocaleService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;

    public CurrentLocaleService(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
    }

    public string GetCurrentLocale()
    {
        // Use HttpContext first so Razor Pages (Login, Register) work — NavigationManager is not initialized there.
        var requestPath = _httpContextAccessor.HttpContext?.Request.Path.Value ?? "";
        var requestSegments = requestPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (requestSegments.Length > 0 &&
            (string.Equals(requestSegments[0], "no", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(requestSegments[0], "en", StringComparison.OrdinalIgnoreCase)))
            return requestSegments[0].ToLowerInvariant();

        // Blazor circuit: NavigationManager is available and reflects client-side URL.
        try
        {
            var path = new Uri(_navigationManager.Uri).AbsolutePath.TrimStart('/');
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 &&
                (string.Equals(segments[0], "no", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(segments[0], "en", StringComparison.OrdinalIgnoreCase)))
                return segments[0].ToLowerInvariant();
        }
        catch (InvalidOperationException)
        {
            // RemoteNavigationManager not initialized (e.g. Razor Page context); already used path above.
        }

        return "no";
    }
}
