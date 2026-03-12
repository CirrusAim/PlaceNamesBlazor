using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("kommuner")]
public class Kommune
{
    [Column("kommune_id")]
    public int KommuneId { get; set; }

    [Column("kommunenavn")]
    public string Kommunenavn { get; set; } = string.Empty;

    [Column("fylke_id")]
    public int FylkeId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey(nameof(FylkeId))]
    public virtual Fylke Fylke { get; set; } = null!;

    public virtual ICollection<Poststed> Poststeder { get; set; } = new List<Poststed>();
}
