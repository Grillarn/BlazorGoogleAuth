namespace BlazorGoogleAuth.Services.EkoWeb;

// Plana klientmodeller som matchar JSON-formen från EkoWebApi. Det här är
// inte EF-entiteter - all databasåtkomst för EkoWeb sker i EkoWebApi, den här
// appen pratar bara HTTP med det API:et (se EkoWebApiClient).

public class Anvandare
{
    public int Id { get; set; }
    public string ExtId { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string Fornamn { get; set; } = string.Empty;
    public string Efternamn { get; set; } = string.Empty;
}

public class Anvandarroll
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;
    public string? Beskrivning { get; set; }
}

public class Institut
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;
    public string? Beskrivning { get; set; }
}

public class Kontotyp
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;
    public bool Externt { get; set; }
}

public class KontoAnvandare
{
    public int Id { get; set; }
    public int AnvandareId { get; set; }
    public Anvandare Anvandare { get; set; } = null!;
    public decimal AndelProcent { get; set; }
}

public class Konto
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;
    public string? KontoNr { get; set; }
    public string? Beskrivning { get; set; }
    public int KontotypId { get; set; }
    public Kontotyp Kontotyp { get; set; } = null!;
    public int InstitutId { get; set; }
    public Institut Institut { get; set; } = null!;
    public List<KontoAnvandare> Anvandare { get; set; } = [];
}

/// <summary>Lättviktsreferens (bara id+namn) för Kategoris Förälder/Ekonomi.</summary>
public class NamedRef
{
    public int Id { get; set; }
    public string? Namn { get; set; }
}

public class Kategori
{
    public int Id { get; set; }
    public string? Namn { get; set; }
    public string? Beskrivning { get; set; }
    public int? ForalderId { get; set; }
    public NamedRef? Foralder { get; set; }
    public int? EkonomiId { get; set; }
    public NamedRef? Ekonomi { get; set; }
}

public class EkonomiAnvandare
{
    public int Id { get; set; }
    public int AnvandareId { get; set; }
    public Anvandare Anvandare { get; set; } = null!;
    public int AnvandarrollId { get; set; }
    public Anvandarroll Anvandarroll { get; set; } = null!;
    public decimal Andel { get; set; }
}

public class Ekonomi
{
    public int Id { get; set; }
    public string Namn { get; set; } = string.Empty;
    public string? Beskrivning { get; set; }
    public int EkonomiAgareId { get; set; }
    public Anvandare EkonomiAgare { get; set; } = null!;
    public int? TransitKontoId { get; set; }
    public List<EkonomiAnvandare> Anvandare { get; set; } = [];
}

public class SenastePeriod
{
    public int Ar { get; set; }
    public int Manad { get; set; }
}

/// <summary>Matchar API:ets wrapper-objekt (se SenastePeriodResponse i EkoWebApi).</summary>
public class SenastePeriodResponse
{
    public SenastePeriod? Period { get; set; }
}

public class Transaktion
{
    public int Id { get; set; }
    public DateOnly Datum { get; set; }
    public int Ar { get; set; }
    public int Manad { get; set; }
    public int FranKontoId { get; set; }
    public NamedRef FranKonto { get; set; } = null!;
    public int TillKontoId { get; set; }
    public NamedRef TillKonto { get; set; } = null!;
    public int KategoriId { get; set; }
    public NamedRef Kategori { get; set; } = null!;
    public int EkonomiId { get; set; }
    public NamedRef Ekonomi { get; set; } = null!;
    public int AnvandareId { get; set; }
    public Anvandare Anvandare { get; set; } = null!;
    public decimal Belopp { get; set; }
    public bool Aterkommande { get; set; }
    public string? Kommentar { get; set; }
}
