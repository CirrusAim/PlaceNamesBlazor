using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace PlaceNamesBlazor.Configuration;

/// <summary>
/// Sets request culture from the first URL path segment when it is "no" or "en".
/// Use with route-based localization (e.g. /no/admin, /en/search).
/// </summary>
public class RouteSegmentRequestCultureProvider : RequestCultureProvider
{
    public const string DefaultLocale = "no";

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 &&
            (string.Equals(segments[0], "no", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(segments[0], "en", StringComparison.OrdinalIgnoreCase)))
        {
            var culture = segments[0].ToLowerInvariant();
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
        }

        return Task.FromResult<ProviderCultureResult?>(null);
    }
}
