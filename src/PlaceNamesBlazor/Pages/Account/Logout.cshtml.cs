using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlaceNamesBlazor.Pages.Account;

public class LogoutModel : PageModel
{
    [FromRoute]
    public string Locale { get; set; } = "no";

    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect($"/{Locale}");
    }
}
