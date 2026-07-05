namespace EkoWebApi.Data.Entities;

/// <summary>
/// Kopplar en användare till en ekonomi med en roll och en andel (%).
/// Motsvarar tabellen Ekonomi_Anvandare.
/// </summary>
public class EkonomiAnvandare
{
    public int Id { get; set; }

    public int EkonomiId { get; set; }

    public Ekonomi Ekonomi { get; set; } = null!;

    /// <summary>Kolumnen heter AnvadareId i databasen (stavfel i originalschemat).</summary>
    public int AnvadareId { get; set; }

    public Anvandare Anvandare { get; set; } = null!;

    public int AnvandarrollId { get; set; }

    public Anvandarroll Anvandarroll { get; set; } = null!;

    public decimal Andel { get; set; }
}
