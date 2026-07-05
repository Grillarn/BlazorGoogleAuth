namespace EkoWebApi.Data.Entities;

public class Institut
{
    public int Id { get; set; }

    public required string Namn { get; set; }

    public string? Beskrivning { get; set; }
}
