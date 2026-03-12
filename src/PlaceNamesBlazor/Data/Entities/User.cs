using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("users")]
public class User
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("middle_name")]
    public string? MiddleName { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "guest";

    [Column("username")]
    public string? Username { get; set; }

    [Column("telephone")]
    public string? Telephone { get; set; }

    [Column("rapportoer_id")]
    public int? RapportoerId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(RapportoerId))]
    public virtual Rapportoer? Rapportoer { get; set; }
}
