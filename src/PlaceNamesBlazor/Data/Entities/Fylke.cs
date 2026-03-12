using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("fylker")]
public class Fylke
{
    [Column("fylke_id")]
    public int FylkeId { get; set; }

    [Column("fylke_navn")]
    public string FylkeNavn { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Kommune> Kommuner { get; set; } = new List<Kommune>();
}
