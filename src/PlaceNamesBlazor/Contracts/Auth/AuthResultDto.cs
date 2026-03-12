namespace PlaceNamesBlazor.Contracts.Auth;

public class AuthResultDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "guest";
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? Telephone { get; set; }
}
