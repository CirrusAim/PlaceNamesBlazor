using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("rapportoer")]
public class Rapportoer
{
    [Column("rapportoer_id")]
    public int RapportoerId { get; set; }

    [Column("initialer")]
    public string Initialer { get; set; } = string.Empty;

    [Column("fornavn_etternavn")]
    public string FornavnEtternavn { get; set; } = string.Empty;

    [Column("telefon")]
    public string? Telefon { get; set; }

    [Column("epost")]
    public string Epost { get; set; } = string.Empty;

    [Column("medlemsklubb")]
    public string? Medlemsklubb { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Rapporteringshistorikk> Rapporteringshistorikk { get; set; } = new List<Rapporteringshistorikk>();
}
