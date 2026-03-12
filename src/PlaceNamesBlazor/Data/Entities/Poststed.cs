using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("poststeder")]
public class Poststed
{
    [Column("poststed_id")]
    public int PoststedId { get; set; }

    [Column("postnummer")]
    public string? Postnummer { get; set; }

    [Column("poststed_navn")]
    public string PoststedNavn { get; set; } = string.Empty;

    [Column("tidligere_navn")]
    public string? TidligereNavn { get; set; }

    [Column("poststed_fra_dato")]
    public string? PoststedFraDato { get; set; }

    [Column("poststed_til_dato")]
    public string? PoststedTilDato { get; set; }

    [Column("tidligere_poststed_id")]
    public int? TidligerePoststedId { get; set; }

    [Column("kommune_id")]
    public int? KommuneId { get; set; }

    [Column("kommentarer")]
    public string? Kommentarer { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(KommuneId))]
    public virtual Kommune? Kommune { get; set; }

    public virtual ICollection<Stempel> Stempler { get; set; } = new List<Stempel>();
}
