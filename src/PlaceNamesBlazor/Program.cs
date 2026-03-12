using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Components;
using PlaceNamesBlazor.Configuration;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Services;
using PlaceNamesBlazor.Services.Audit;
using PlaceNamesBlazor.Services.Admin;
using PlaceNamesBlazor.Services.Auth;
using PlaceNamesBlazor.Services.ImageStorage;
using PlaceNamesBlazor.Services.Record;
using PlaceNamesBlazor.Services.Reporter;
using PlaceNamesBlazor.Services.Reporting;
using PlaceNamesBlazor.Services.Locale;
using PlaceNamesBlazor.Services.StampType;
using PlaceNamesBlazor.Services.Subcategory;
using PlaceNamesBlazor.Services.UsagePeriod;
using Microsoft.AspNetCore.Localization;

// App setup: Blazor Server, session, DB (local vs DATABASE_URL), cookie auth, image storage (Local/Cloudflare), hosted schema/admin.
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var connectionString = DatabaseConnectionFactory.GetConnectionString(builder.Configuration);
builder.Services.AddDbContextFactory<PlaceNamesDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<PlaceNamesDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<PlaceNamesDbContext>>().CreateDbContext());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/no/Account/Login";
        options.LogoutPath = "/no/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = ctx =>
        {
            var locale = GetLocaleFromPath(ctx.Request.Path) ?? "no";
            var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
            ctx.Response.Redirect($"/{locale}/Account/Login?returnUrl={returnUrl}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogout = ctx =>
        {
            var locale = GetLocaleFromPath(ctx.Request.Path) ?? "no";
            ctx.Response.Redirect($"/{locale}");
            return Task.CompletedTask;
        };
    });

static string? GetLocaleFromPath(PathString path)
{
    var segment = path.Value?.TrimStart('/').Split('/').FirstOrDefault();
    return segment is "no" or "en" ? segment : null;
}
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", p => p.RequireRole("admin"))
    .AddPolicy("AdminOrSuperuser", p => p.RequireRole("admin", "superuser"));

var imageProvider = (builder.Configuration["ImageStorage:Provider"] ?? "Local").Trim();
if (string.Equals(imageProvider, "Cloudflare", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IImageStorageService, CloudflareR2ImageStorageService>();
else
    builder.Services.AddSingleton<IImageStorageService, LocalImageStorageService>();

builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ILoginLockoutService, LoginLockoutService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<IStampSearchService, StampSearchService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReporterService, ReporterService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<IBatchImportService, BatchImportService>();
builder.Services.AddScoped<ISubcategoryService, SubcategoryService>();
builder.Services.AddScoped<IStampTypeService, StampTypeService>();
builder.Services.AddScoped<IUsagePeriodService, UsagePeriodService>();
builder.Services.AddHostedService<EnsureDatabaseSchemaHostedService>();
builder.Services.AddHostedService<EnsureRapportoerSchemaHostedService>();
builder.Services.AddHostedService<EnsureAuditLogsTableHostedService>();
builder.Services.AddHostedService<EnsureAdminHostedService>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<Microsoft.Extensions.Localization.IStringLocalizer<PlaceNamesBlazor.Resources.SharedResource>, PlaceNamesBlazor.Resources.SharedResourceLocalizer>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("no");
    options.SupportedCultures = [new CultureInfo("no"), new CultureInfo("en")];
    options.SupportedUICultures = [new CultureInfo("no"), new CultureInfo("en")];
    options.RequestCultureProviders.Insert(0, new RouteSegmentRequestCultureProvider());
});

builder.Services.AddScoped<ICurrentLocaleService, CurrentLocaleService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/no/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/no/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Rewrite /framework/* to /_framework/* (fixes 404 when something requests path without leading underscore, e.g. old Assets resolution or proxy)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/framework/", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/_framework/" + path.Substring("/framework/".Length);
    }
    await next(context);
});

// Route-based locale: redirect root to default locale
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" || context.Request.Path.Value == "/")
    {
        context.Response.Redirect("/no");
        return;
    }
    await next(context);
});

app.UseRequestLocalization();
app.UseSession();
app.UseAuthentication();

// Guest search limit: stable cookie so count survives refresh.
app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        const string cookieName = "guest_search_id";
        const int cookieMaxAgeMinutes = 20;
        if (context.Request.Cookies.TryGetValue(cookieName, out var existingId) && !string.IsNullOrEmpty(existingId))
            context.Items["GuestSearchId"] = existingId;
        else
        {
            var newId = Guid.NewGuid().ToString("N");
            context.Items["GuestSearchId"] = newId;
            context.Response.Cookies.Append(cookieName, newId, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(cookieMaxAgeMinutes),
                IsEssential = true
            });
        }
    }
    await next(context);
});

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
