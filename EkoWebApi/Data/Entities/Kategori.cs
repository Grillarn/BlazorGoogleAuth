namespace EkoWebApi.Data.Entities;

public class Kategori
{
    public int Id { get; set; }

    /// <summary>Förälderkategori, om denna kategori är en underkategori.</summary>
    public int? ForalderId { get; set; }

    public Kategori? Foralder { get; set; }

    public string? Namn { get; set; }

    public string? Beskrivning { get; set; }

    public int? EkonomiId { get; set; }

    public Ekonomi? Ekonomi { get; set; }
}
