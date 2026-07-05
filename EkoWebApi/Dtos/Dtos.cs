namespace EkoWebApi.Dtos;

// Alla DTO:er är medvetet plana (max en nivå nästlade objekt) för att undvika
// cirkulära referenser vid JSON-serialisering - EF-entiteternas fulla,
// dubbelriktade navigering stannar internt i API:et.

public record AnvandareDto(int Id, string ExtId, string Mail, string Fornamn, string Efternamn);

public record AnvandarrollDto(int Id, string Namn, string? Beskrivning);

public record InstitutDto(int Id, string Namn, string? Beskrivning);
public record InstitutRequest(string Namn, string? Beskrivning);

public record KontotypDto(int Id, string Namn, bool Externt);
public record KontotypRequest(string Namn, bool Externt);

public record KontoAnvandareDto(int Id, int AnvandareId, AnvandareDto Anvandare, decimal AndelProcent);
public record KontoAnvandareRequest(int AnvandareId, decimal AndelProcent);

public record KontoDto(
    int Id,
    string Namn,
    string? KontoNr,
    string? Beskrivning,
    int KontotypId,
    KontotypDto Kontotyp,
    int InstitutId,
    InstitutDto Institut,
    List<KontoAnvandareDto> Anvandare);
public record KontoRequest(string Namn, string? KontoNr, string? Beskrivning, int KontotypId, int InstitutId);

/// <summary>Lättviktsreferens (bara id+namn) för att undvika djup nästling.</summary>
public record NamedRefDto(int Id, string? Namn);

public record KategoriDto(
    int Id,
    string? Namn,
    string? Beskrivning,
    int? ForalderId,
    NamedRefDto? Foralder,
    int? EkonomiId,
    NamedRefDto? Ekonomi);
public record KategoriRequest(string? Namn, string? Beskrivning, int? ForalderId, int? EkonomiId);

public record EkonomiAnvandareDto(int Id, int AnvandareId, AnvandareDto Anvandare, int AnvandarrollId, AnvandarrollDto Anvandarroll, decimal Andel);
public record EkonomiAnvandareRequest(int AnvandareId, int AnvandarrollId, decimal Andel);

public record EkonomiDto(
    int Id,
    string Namn,
    string? Beskrivning,
    int EkonomiAgareId,
    AnvandareDto EkonomiAgare,
    int? TransitKontoId,
    List<EkonomiAnvandareDto> Anvandare);
public record EkonomiRequest(string Namn, string? Beskrivning, int EkonomiAgareId, int? TransitKontoId);
