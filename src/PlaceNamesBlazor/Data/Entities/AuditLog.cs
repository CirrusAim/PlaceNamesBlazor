using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("audit_id")]
    public int AuditId { get; set; }

    [Column("actor_id")]
    public int ActorId { get; set; }

    [Column("actor_email")]
    public string ActorEmail { get; set; } = string.Empty;

    [Column("actor_role")]
    public string ActorRole { get; set; } = string.Empty;

    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;

    [Column("target_type")]
    public string? TargetType { get; set; }

    [Column("target_id")]
    public int? TargetId { get; set; }

    [Column("target_description")]
    public string? TargetDescription { get; set; }

    [Column("details", TypeName = "jsonb")]
    public string? DetailsJson { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(ActorId))]
    public virtual User? Actor { get; set; }
}
