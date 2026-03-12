using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("rapporteringshistorikk")]
public class Rapporteringshistorikk
{
    [Column("rapporteringshistorikk_id")]
    public int RapporteringshistorikkId { get; set; }

    [Column("stempel_id")]
    public int StempelId { get; set; }

    [Column("bruksperiode_id")]
    public int BruksperiodeId { get; set; }

    [Column("rapporteringsdato")]
    public DateOnly Rapporteringsdato { get; set; }

    [Column("rapportoer_id")]
    public int RapportoerId { get; set; }

    [Column("rapportering_foerste_siste_dato")]
    public string RapporteringFoersteSisteDato { get; set; } = "F";

    [Column("dato_for_rapportert_avtrykk")]
    public string DatoForRapportertAvtrykk { get; set; } = string.Empty;

    [Column("godkjent_forkastet")]
    public string? GodkjentForkastet { get; set; }

    [Column("besluttet_dato")]
    public DateOnly? BesluttetDato { get; set; }

    [Column("initialer_beslutter")]
    public string? InitialerBeslutter { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(StempelId))]
    public virtual Stempel Stempel { get; set; } = null!;

    [ForeignKey(nameof(BruksperiodeId))]
    public virtual Bruksperiode Bruksperiode { get; set; } = null!;

    [ForeignKey(nameof(RapportoerId))]
    public virtual Rapportoer Rapportoer { get; set; } = null!;

    public virtual ICollection<RapporteringshistorikkBilde> Bilder { get; set; } = new List<RapporteringshistorikkBilde>();
}
