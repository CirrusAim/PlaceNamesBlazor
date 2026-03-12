using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("bruksperioder_bilder")]
public class BruksperiodeBilde
{
    [Key]
    [Column("bilde_id")]
    public int BildeId { get; set; }

    [Column("bruksperiode_id")]
    public int BruksperiodeId { get; set; }

    [Column("bilde_path")]
    public string BildePath { get; set; } = string.Empty;

    [Column("bilde_filnavn")]
    public string? BildeFilnavn { get; set; }

    [Column("bilde_nummer")]
    public int BildeNummer { get; set; }

    [Column("beskrivelse")]
    public string? Beskrivelse { get; set; }

    [Column("opplastet_dato")]
    public DateTime? OpplastetDato { get; set; }

    [Column("opplastet_av")]
    public string? OpplastetAv { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey(nameof(BruksperiodeId))]
    public virtual Bruksperiode Bruksperiode { get; set; } = null!;
}
