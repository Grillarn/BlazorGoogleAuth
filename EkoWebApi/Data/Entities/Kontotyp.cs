namespace EkoWebApi.Data.Entities;

public class Kontotyp
{
    public int Id { get; set; }

    public required string Namn { get; set; }

    /// <summary>
    /// True om kontotypen är extern (t.ex. arbetsgivare, betalningsmottagare)
    /// snarare än ett eget konto.
    /// </summary>
    public bool Externt { get; set; }
}
