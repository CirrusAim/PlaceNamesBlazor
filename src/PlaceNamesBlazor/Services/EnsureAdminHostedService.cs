using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;
using PlaceNamesBlazor.Services.Auth;

namespace PlaceNamesBlazor.Services;

/// <summary>Ensures at least one admin user from ADMIN_EMAIL/ADMIN_PASSWORD at startup. No-op if an admin already exists.</summary>
public class EnsureAdminHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public EnsureAdminHostedService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = config["ADMIN_EMAIL"]?.Trim();
        var password = config["ADMIN_PASSWORD"]?.Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return;
        var (pwdValid, _) = PasswordPolicy.Validate(password);
        if (!pwdValid)
            return;

        var db = scope.ServiceProvider.GetRequiredService<PlaceNamesDbContext>();
        var hasAdmin = await db.Users.AnyAsync(u => u.Role == "admin", cancellationToken);
        if (hasAdmin)
            return;

        var normalized = email.ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == normalized, cancellationToken))
            return;

        var user = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = normalized,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10),
            Role = "admin",
            Username = "admin",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
