namespace EkoWebApi.Data.Entities;

/// <summary>
/// Datumdimension (kalendertabell), förifylld av EkoWeb från 1900 till 2173.
/// Hanteras inte (skapas/tas bort) härifrån - bara läses för att slå upp
/// DatumId till ett givet datum, eller för att gruppera transaktioner
/// per år/månad. Kolumnerna År/Månad heter Ar/Manad i C# (ASCII) men mappas
/// till de riktiga kolumnnamnen i EkoWebDbContext.
/// </summary>
public class Datum
{
    public int Id { get; set; }

    /// <summary>Kolumnen heter Datum i databasen (kan inte hetera samma som klassen i C#).</summary>
    public DateOnly Kalenderdatum { get; set; }

    public int? Ar { get; set; }

    public int? Manad { get; set; }

    public int? Dag { get; set; }

    public int? Kvartal { get; set; }

    public int? Tertial { get; set; }

    public int? Veckodag { get; set; }

    public string? StrVeckodag { get; set; }

    public bool? Helgdag { get; set; }
}
