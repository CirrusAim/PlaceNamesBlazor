using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("stempelbilder")]
public class Stempelbilde
{
    [Key]
    [Column("bilde_id")]
    public int BildeId { get; set; }

    [Column("stempel_id")]
    public int StempelId { get; set; }

    [Column("bilde_path")]
    public string BildePath { get; set; } = string.Empty;

    [Column("bilde_filnavn")]
    public string? BildeFilnavn { get; set; }

    [Column("er_primær")]
    public bool ErPrimær { get; set; }

    [Column("beskrivelse")]
    public string? Beskrivelse { get; set; }

    [Column("opplastet_dato")]
    public DateTime? OpplastetDato { get; set; }

    [Column("opplastet_av")]
    public string? OpplastetAv { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey(nameof(StempelId))]
    public virtual Stempel Stempel { get; set; } = null!;
}
