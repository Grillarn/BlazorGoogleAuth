namespace EkoWebApi.Data.Entities;

public class Konto
{
    public int Id { get; set; }

    public required string Namn { get; set; }

    public string? KontoNr { get; set; }

    public string? Beskrivning { get; set; }

    public int KontotypId { get; set; }

    public Kontotyp Kontotyp { get; set; } = null!;

    public int InstitutId { get; set; }

    public Institut Institut { get; set; } = null!;

    public List<KontoAnvandare> Anvandare { get; set; } = [];
}
