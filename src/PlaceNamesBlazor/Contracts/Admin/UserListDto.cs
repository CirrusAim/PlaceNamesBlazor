namespace PlaceNamesBlazor.Contracts.Admin;

public class UserListDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Username { get; set; }
    public string? Telephone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? RapportoerId { get; set; }
    public string? ReporterStatus { get; set; }
    public string? ReporterInitialer { get; set; }
    public string? ReporterFornavnEtternavn { get; set; }
    /// <summary>Email of the admin who approved this user as reporter (from audit log).</summary>
    public string? ApprovedByEmail { get; set; }
}
