using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlaceNamesBlazor.Services.Auth;

namespace PlaceNamesBlazor.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    [FromRoute]
    public string Locale { get; set; } = "no";

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("admin") || User.IsInRole("superuser"))
                return Redirect($"/{Locale}/admin");
            return Redirect($"/{Locale}");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostLoginAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required.";
            return Page();
        }

        var (user, error, lockedUntilUtc) = await _authService.ValidateLoginAsync(Email, Password, cancellationToken);
        if (lockedUntilUtc.HasValue)
        {
            ErrorMessage = error ?? "Account temporarily locked. Try again later.";
            return Page();
        }
        if (user == null)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        var principal = _authService.CreatePrincipal(user);
        var props = new AuthenticationProperties { IsPersistent = true };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        HttpContext.Session.Remove("GuestSearchCount");
        HttpContext.Response.Cookies.Delete("guest_search_id", new CookieOptions { Path = "/" });
        if (user.Role is "admin" or "superuser")
            return Redirect($"/{Locale}/admin");
        return Redirect($"/{Locale}");
    }
}
