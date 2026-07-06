namespace EkoWebApi.Data.Entities;

public class Transaktion
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int DatumId { get; set; }

    public Datum Datum { get; set; } = null!;

    public int FranKontoId { get; set; }

    public Konto FranKonto { get; set; } = null!;

    public int TillKontoId { get; set; }

    public Konto TillKonto { get; set; } = null!;

    public int KategoriId { get; set; }

    public Kategori Kategori { get; set; } = null!;

    public int EkonomiId { get; set; }

    public Ekonomi Ekonomi { get; set; } = null!;

    public int AnvandareId { get; set; }

    public Anvandare Anvandare { get; set; } = null!;

    public decimal Belopp { get; set; }

    /// <summary>Kolumnen heter Återkommande i databasen.</summary>
    public bool Aterkommande { get; set; }

    public string? Kommentar { get; set; }
}
