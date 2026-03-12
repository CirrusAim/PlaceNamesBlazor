using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlaceNamesBlazor.Contracts.Auth;
using PlaceNamesBlazor.Services.Auth;

namespace PlaceNamesBlazor.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;

    public RegisterModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    [Required, MinLength(1)]
    public string FirstName { get; set; } = "";

    [BindProperty]
    [Required, MinLength(1)]
    public string LastName { get; set; } = "";

    [BindProperty]
    public string? MiddleName { get; set; }

    [BindProperty]
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [BindProperty]
    [Required, MinLength(1)]
    public string Username { get; set; } = "";

    [BindProperty]
    [Required, MinLength(PasswordPolicy.MinLength)]
    public string Password { get; set; } = "";

    [BindProperty]
    public string? Telephone { get; set; }

    public string? ErrorMessage { get; set; }

    [FromRoute]
    public string Locale { get; set; } = "no";

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect($"/{Locale}");
        return Page();
    }

    public async Task<IActionResult> OnPostRegisterAsync(CancellationToken cancellationToken)
    {
        var (pwdValid, pwdError) = PasswordPolicy.Validate(Password);
        if (!pwdValid)
        {
            ErrorMessage = pwdError ?? "Password does not meet policy.";
            return Page();
        }
        var request = new RegisterRequest
        {
            FirstName = FirstName,
            LastName = LastName,
            MiddleName = MiddleName,
            Email = Email,
            Username = Username,
            Password = Password,
            Telephone = Telephone
        };

        var user = await _authService.RegisterAndGetUserAsync(request, cancellationToken);
        if (user == null)
        {
            ErrorMessage = "Email or username already registered.";
            return Page();
        }

        var principal = _authService.CreatePrincipal(user);
        var props = new AuthenticationProperties { IsPersistent = true };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        return Redirect($"/{Locale}");
    }
}
