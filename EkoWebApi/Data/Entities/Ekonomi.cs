namespace EkoWebApi.Data.Entities;

public class Ekonomi
{
    public int Id { get; set; }

    public required string Namn { get; set; }

    public string? Beskrivning { get; set; }

    /// <summary>Användaren som "äger" ekonomin.</summary>
    public int EkonomiAgareId { get; set; }

    public Anvandare EkonomiAgare { get; set; } = null!;

    /// <summary>Valfritt transitkonto kopplat till ekonomin (ingen databas-FK, bara en referens till Konto.Id).</summary>
    public int? TransitKontoId { get; set; }

    public List<EkonomiAnvandare> Anvandare { get; set; } = [];
}
