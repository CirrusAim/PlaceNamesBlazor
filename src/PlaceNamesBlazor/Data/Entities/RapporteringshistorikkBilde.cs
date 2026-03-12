using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("rapporteringshistorikk_bilder")]
public class RapporteringshistorikkBilde
{
    [Key]
    [Column("bilde_id")]
    public int BildeId { get; set; }

    [Column("rapporteringshistorikk_id")]
    public int RapporteringshistorikkId { get; set; }

    [Column("bilde_path")]
    public string BildePath { get; set; } = string.Empty;

    [Column("bilde_filnavn")]
    public string? BildeFilnavn { get; set; }

    [Column("beskrivelse")]
    public string? Beskrivelse { get; set; }

    [Column("opplastet_dato")]
    public DateTime? OpplastetDato { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey(nameof(RapporteringshistorikkId))]
    public virtual Rapporteringshistorikk Rapporteringshistorikk { get; set; } = null!;
}
