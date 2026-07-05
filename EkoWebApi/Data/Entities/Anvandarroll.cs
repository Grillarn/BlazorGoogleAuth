namespace EkoWebApi.Data.Entities;

/// <summary>Rolltyp för en användares koppling till en ekonomi, t.ex. "Standard".</summary>
public class Anvandarroll
{
    public int Id { get; set; }

    public required string Namn { get; set; }

    public string? Beskrivning { get; set; }
}
