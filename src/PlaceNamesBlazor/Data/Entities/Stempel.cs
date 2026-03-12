using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("stempler")]
public class Stempel
{
    [Column("stempel_id")]
    public int StempelId { get; set; }

    [Column("poststed_id")]
    public int PoststedId { get; set; }

    [Column("stempeltype_id")]
    public int StempeltypeId { get; set; }

    [Column("underkategori_id")]
    public int? UnderkategoriId { get; set; }

    [Column("stempeltekst_oppe")]
    public string StempeltekstOppe { get; set; } = string.Empty;

    [Column("stempeltekst_nede")]
    public string? StempeltekstNede { get; set; }

    [Column("stempeltekst_midt")]
    public string? StempeltekstMidt { get; set; }

    [Column("stempelgravoer")]
    public string? Stempelgravoer { get; set; }

    [Column("dato_fra_gravoer")]
    public DateOnly? DatoFraGravoer { get; set; }

    [Column("dato_fra_intendantur_til_overordnet_postkontor")]
    public DateOnly? DatoFraIntendanturTilOverordnetPostkontor { get; set; }

    [Column("dato_fra_overordnet_postkontor")]
    public DateOnly? DatoFraOverordnetPostkontor { get; set; }

    [Column("dato_for_innlevering_til_overordnet_postkontor")]
    public DateOnly? DatoForInnleveringTilOverordnetPostkontor { get; set; }

    [Column("dato_innlevert_intendantur")]
    public DateOnly? DatoInnlevertIntendantur { get; set; }

    [Column("tapsmelding")]
    public string? Tapsmelding { get; set; }

    [Column("stempeldiameter")]
    public decimal? Stempeldiameter { get; set; }

    [Column("bokstavhoeyde")]
    public decimal? Bokstavhoeyde { get; set; }

    [Column("andre_maal")]
    public string? AndreMaal { get; set; }

    [Column("stempelfarge")]
    public string? Stempelfarge { get; set; }

    [Column("reparasjoner")]
    public string? Reparasjoner { get; set; }

    [Column("dato_avtrykk_i_pm")]
    public string? DatoAvtrykkIPm { get; set; }

    [Column("kommentar")]
    public string? Kommentar { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(PoststedId))]
    public virtual Poststed Poststed { get; set; } = null!;

    [ForeignKey(nameof(StempeltypeId))]
    public virtual Stempeltype Stempeltype { get; set; } = null!;

    [ForeignKey(nameof(UnderkategoriId))]
    public virtual UnderkategoriStempeltype? Underkategori { get; set; }

    public virtual ICollection<Bruksperiode> Bruksperioder { get; set; } = new List<Bruksperiode>();
    public virtual ICollection<Stempelbilde> Stempelbilder { get; set; } = new List<Stempelbilde>();
    public virtual ICollection<Rapporteringshistorikk> Rapporteringshistorikk { get; set; } = new List<Rapporteringshistorikk>();
}
