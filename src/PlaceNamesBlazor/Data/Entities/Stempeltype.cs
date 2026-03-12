using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("stempeltyper")]
public class Stempeltype
{
    [Column("stempeltype_id")]
    public int StempeltypeId { get; set; }

    [Column("hovedstempeltype")]
    public string Hovedstempeltype { get; set; } = string.Empty;

    [Column("stempeltype_full_tekst")]
    public string StempeltypeFullTekst { get; set; } = string.Empty;

    [Column("maanedsangivelse_type")]
    public string MaanedsangivelseType { get; set; } = "A";

    [Column("stempelutfoerelse")]
    public string Stempelutfoerelse { get; set; } = "S";

    [Column("skrifttype")]
    public string? Skrifttype { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Stempel> Stempler { get; set; } = new List<Stempel>();
    public virtual ICollection<UnderkategoriStempeltype> Underkategorier { get; set; } = new List<UnderkategoriStempeltype>();
}
