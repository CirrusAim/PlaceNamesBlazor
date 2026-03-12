using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlaceNamesBlazor.Data.Entities;

[Table("underkategori_stempeltyper")]
public class UnderkategoriStempeltype
{
    [Key]
    [Column("underkategori_id")]
    public int UnderkategoriId { get; set; }

    [Column("stempeltype_id")]
    public int StempeltypeId { get; set; }

    [Column("underkategori")]
    public string Underkategori { get; set; } = string.Empty;

    [Column("underkategori_full_tekst")]
    public string? UnderkategoriFullTekst { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(StempeltypeId))]
    public virtual Stempeltype Stempeltype { get; set; } = null!;
}
