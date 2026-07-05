namespace EkoWebApi.Data.Entities;

/// <summary>
/// Kopplar en användare till ett konto med en andel (%).
/// Motsvarar tabellen Konto_Anvadare (stavfel i originalschemat, tabellnamnet
/// saknar ett "n" - kolumnnamnen är dock korrekt stavade).
/// </summary>
public class KontoAnvandare
{
    public int Id { get; set; }

    public int KontoId { get; set; }

    public Konto Konto { get; set; } = null!;

    public int AnvandareId { get; set; }

    public Anvandare Anvandare { get; set; } = null!;

    public decimal AndelProcent { get; set; }
}
