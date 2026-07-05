namespace EkoWebApi.Data.Entities;

/// <summary>
/// EkoWebs egen användartabell. Kopplas till BlazorGoogleAuths Users-tabell
/// via ExtId (som håller Google-kontots GoogleId). Hanteras inte
/// (skapas/tas bort) härifrån, bara läses för att kunna koppla ihop
/// Ekonomi/Konto med rätt person.
/// </summary>
public class Anvandare
{
    public int Id { get; set; }

    public required string ExtId { get; set; }

    public required string Mail { get; set; }

    public required string Fornamn { get; set; }

    public required string Efternamn { get; set; }
}
