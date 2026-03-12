using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("bruksperioder")]
public class Bruksperiode
{
    [Column("bruksperiode_id")]
    public int BruksperiodeId { get; set; }

    [Column("stempel_id")]
    public int StempelId { get; set; }

    [Column("bruksperiode_fra")]
    public string? BruksperiodeFra { get; set; }

    [Column("bruksperiode_til")]
    public string? BruksperiodeTil { get; set; }

    [Column("dato_foerste_kjente_bruksdato")]
    public string? DatoFoersteKjenteBruksdato { get; set; }

    [Column("dato_foerste_kjente_bruksdato_tillegg")]
    public DateOnly? DatoFoersteKjenteBruksdatoTillegg { get; set; }

    [Column("rapportoer_id_foerste_bruksdato")]
    public int? RapportoerIdFoersteBruksdato { get; set; }

    [Column("dato_siste_kjente_bruksdato")]
    public string? DatoSisteKjenteBruksdato { get; set; }

    [Column("dato_siste_kjente_bruksdato_tillegg")]
    public DateOnly? DatoSisteKjenteBruksdatoTillegg { get; set; }

    [Column("rapportoer_id_siste_bruksdato")]
    public int? RapportoerIdSisteBruksdato { get; set; }

    [Column("kommentarer")]
    public string? Kommentarer { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(StempelId))]
    public virtual Stempel Stempel { get; set; } = null!;

    [ForeignKey(nameof(RapportoerIdFoersteBruksdato))]
    public virtual Rapportoer? RapportoerFoerste { get; set; }

    [ForeignKey(nameof(RapportoerIdSisteBruksdato))]
    public virtual Rapportoer? RapportoerSiste { get; set; }

    public virtual ICollection<BruksperiodeBilde> Bilder { get; set; } = new List<BruksperiodeBilde>();
    public virtual ICollection<Rapporteringshistorikk> Rapporteringshistorikk { get; set; } = new List<Rapporteringshistorikk>();
}
