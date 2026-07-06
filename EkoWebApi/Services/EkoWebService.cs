using Microsoft.EntityFrameworkCore;
using EkoWebApi.Data;
using EkoWebApi.Data.Entities;

namespace EkoWebApi.Services;

public interface IEkoWebService
{
    // --- Läs-uppslag (för dropdowns m.m.) ---
    Task<List<Anvandare>> GetAnvandareAsync();
    Task<List<Anvandarroll>> GetAnvandarrollerAsync();

    // --- Institut ---
    Task<List<Institut>> GetInstitutAsync();
    Task<(bool Success, string? Error)> CreateInstitutAsync(string namn, string? beskrivning);
    Task<(bool Success, string? Error)> UpdateInstitutAsync(int id, string namn, string? beskrivning);
    Task<(bool Success, string? Error)> DeleteInstitutAsync(int id);

    // --- Kontotyp ---
    Task<List<Kontotyp>> GetKontotyperAsync();
    Task<(bool Success, string? Error)> CreateKontotypAsync(string namn, bool externt);
    Task<(bool Success, string? Error)> UpdateKontotypAsync(int id, string namn, bool externt);
    Task<(bool Success, string? Error)> DeleteKontotypAsync(int id);

    // --- Konto (inkl. koppling till användare via Konto_Anvadare) ---
    /// <summary>
    /// Hämtar konton. Om <paramref name="callerIsAdmin"/> är false filtreras
    /// listan till bara de konton som <paramref name="callerExtId"/> (Anvandare.ExtId)
    /// är kopplad till - så vanliga användare bara ser sina egna konton.
    /// </summary>
    Task<List<Konto>> GetKontonAsync(string? callerExtId, bool callerIsAdmin);
    Task<(bool Success, string? Error)> CreateKontoAsync(string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId);
    Task<(bool Success, string? Error)> UpdateKontoAsync(int id, string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId);
    Task<(bool Success, string? Error)> DeleteKontoAsync(int id);
    Task<(bool Success, string? Error)> AddKontoAnvandareAsync(int kontoId, int anvandareId, decimal andelProcent);
    Task RemoveKontoAnvandareAsync(int linkId);

    // --- Kategori ---
    /// <summary>
    /// Hämtar kategorier. Om <paramref name="callerIsAdmin"/> är false filtreras
    /// listan till kategorier vars Ekonomi ägs av eller är delad med
    /// <paramref name="callerExtId"/> - kategorier utan Ekonomi (globala) visas
    /// aldrig för icke-admin.
    /// </summary>
    Task<List<Kategori>> GetKategorierAsync(string? callerExtId, bool callerIsAdmin);
    Task<(bool Success, string? Error)> CreateKategoriAsync(string? namn, string? beskrivning, int? foralderId, int? ekonomiId);
    Task<(bool Success, string? Error)> UpdateKategoriAsync(int id, string? namn, string? beskrivning, int? foralderId, int? ekonomiId);
    Task<(bool Success, string? Error)> DeleteKategoriAsync(int id);

    // --- Ekonomi (inkl. koppling till användare via Ekonomi_Anvandare) ---
    /// <summary>
    /// Hämtar ekonomier. Om <paramref name="callerIsAdmin"/> är false filtreras
    /// listan till ekonomier där <paramref name="callerExtId"/> är ägare eller
    /// kopplad via Ekonomi_Anvandare.
    /// </summary>
    Task<List<Ekonomi>> GetEkonomierAsync(string? callerExtId, bool callerIsAdmin);
    Task<(bool Success, string? Error)> CreateEkonomiAsync(string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId);
    Task<(bool Success, string? Error)> UpdateEkonomiAsync(int id, string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId);
    Task<(bool Success, string? Error)> DeleteEkonomiAsync(int id);
    Task<(bool Success, string? Error)> AddEkonomiAnvandareAsync(int ekonomiId, int anvandareId, int anvandarrollId, decimal andel);
    Task RemoveEkonomiAnvandareAsync(int linkId);

    // --- Transaktion ---
    /// <summary>
    /// Hämtar transaktioner, valfritt filtrerat på år/månad. Om
    /// <paramref name="callerIsAdmin"/> är false filtreras listan till
    /// transaktioner vars Ekonomi ägs av eller är delad med <paramref name="callerExtId"/>.
    /// </summary>
    Task<List<Transaktion>> GetTransaktionerAsync(string? callerExtId, bool callerIsAdmin, int? ar, int? manad);

    /// <summary>Vilka år som har minst en transaktion (för år-väljaren), inom samma behörighetsscope.</summary>
    Task<List<int>> GetTransaktionArAsync(string? callerExtId, bool callerIsAdmin);

    /// <summary>Vilka månader ett givet år som har minst en transaktion (för månad-väljaren), inom samma behörighetsscope.</summary>
    Task<List<int>> GetTransaktionManaderAsync(string? callerExtId, bool callerIsAdmin, int ar);

    /// <summary>
    /// Den senaste år/månad-perioden som har minst en transaktion, inom samma
    /// behörighetsscope. Null om det inte finns några transaktioner alls.
    /// Används för att föreslå "nästa period" när man ska fylla i nya transaktioner.
    /// </summary>
    Task<(int Ar, int Manad)?> GetSenasteTransaktionsperiodAsync(string? callerExtId, bool callerIsAdmin);

    /// <summary>
    /// Skapar en transaktion. Om <paramref name="callerIsAdmin"/> är false måste
    /// anroparen ha tillgång till <paramref name="ekonomiId"/> (ägare eller delad),
    /// och användaren sätts alltid till anroparens egen Anvandare (kan inte
    /// registrera transaktioner "som" någon annan).
    /// </summary>
    Task<(bool Success, string? Error)> CreateTransaktionAsync(
        DateOnly datum, int franKontoId, int tillKontoId, int kategoriId, int ekonomiId,
        int? anvandareId, decimal belopp, bool aterkommande, string? kommentar,
        string? callerExtId, bool callerIsAdmin);

    Task<(bool Success, string? Error)> UpdateTransaktionAsync(
        int id, DateOnly datum, int franKontoId, int tillKontoId, int kategoriId, int ekonomiId,
        int anvandareId, decimal belopp, bool aterkommande, string? kommentar);

    Task<(bool Success, string? Error)> DeleteTransaktionAsync(int id);
}

public class EkoWebService : IEkoWebService
{
    private readonly EkoWebDbContext _db;

    public EkoWebService(EkoWebDbContext db)
    {
        _db = db;
    }

    public async Task<List<Anvandare>> GetAnvandareAsync()
        => await _db.Anvandare.OrderBy(a => a.Fornamn).ToListAsync();

    public async Task<List<Anvandarroll>> GetAnvandarrollerAsync()
        => await _db.Anvandarroller.OrderBy(r => r.Namn).ToListAsync();

    // --- Institut ---

    public async Task<List<Institut>> GetInstitutAsync()
        => await _db.Institut.OrderBy(b => b.Namn).ToListAsync();

    public async Task<(bool Success, string? Error)> CreateInstitutAsync(string namn, string? beskrivning)
    {
        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        _db.Institut.Add(new Institut { Namn = namn, Beskrivning = NullIfEmpty(beskrivning) });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateInstitutAsync(int id, string namn, string? beskrivning)
    {
        var institut = await _db.Institut.FindAsync(id);
        if (institut is null)
        {
            return (false, "Institutet hittades inte.");
        }

        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        institut.Namn = namn;
        institut.Beskrivning = NullIfEmpty(beskrivning);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteInstitutAsync(int id)
    {
        var institut = await _db.Institut.FindAsync(id);
        if (institut is null)
        {
            return (false, "Institutet hittades inte.");
        }

        var inUse = await _db.Konton.AnyAsync(k => k.InstitutId == id);
        if (inUse)
        {
            return (false, $"Institutet \"{institut.Namn}\" används av minst ett konto och kan inte tas bort.");
        }

        _db.Institut.Remove(institut);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // --- Kontotyp ---

    public async Task<List<Kontotyp>> GetKontotyperAsync()
        => await _db.Kontotyper.OrderBy(k => k.Namn).ToListAsync();

    public async Task<(bool Success, string? Error)> CreateKontotypAsync(string namn, bool externt)
    {
        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        _db.Kontotyper.Add(new Kontotyp { Namn = namn, Externt = externt });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateKontotypAsync(int id, string namn, bool externt)
    {
        var kontotyp = await _db.Kontotyper.FindAsync(id);
        if (kontotyp is null)
        {
            return (false, "Kontotypen hittades inte.");
        }

        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        kontotyp.Namn = namn;
        kontotyp.Externt = externt;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteKontotypAsync(int id)
    {
        var kontotyp = await _db.Kontotyper.FindAsync(id);
        if (kontotyp is null)
        {
            return (false, "Kontotypen hittades inte.");
        }

        var inUse = await _db.Konton.AnyAsync(k => k.KontotypId == id);
        if (inUse)
        {
            return (false, $"Kontotypen \"{kontotyp.Namn}\" används av minst ett konto och kan inte tas bort.");
        }

        _db.Kontotyper.Remove(kontotyp);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // --- Konto ---

    public async Task<List<Konto>> GetKontonAsync(string? callerExtId, bool callerIsAdmin)
    {
        var query = _db.Konton
            .Include(k => k.Kontotyp)
            .Include(k => k.Institut)
            .Include(k => k.Anvandare).ThenInclude(ka => ka.Anvandare)
            .AsQueryable();

        if (!callerIsAdmin)
        {
            // Notera: om callerExtId är null/tom matchar det första villkoret
            // inget (ExtId är aldrig null/tom i databasen) - dvs. "neka som
            // standard" istället för att av misstag visa alla konton.
            // Externa konton (arbetsgivare, betalningsmottagare m.m.) är inte
            // kopplade till någon specifik person och visas alltid, eftersom
            // man annars inte skulle kunna registrera en transaktion mot dem.
            query = query.Where(k => k.Anvandare.Any(ka => ka.Anvandare.ExtId == callerExtId) || k.Kontotyp.Externt);
        }

        return await query.OrderBy(k => k.Namn).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateKontoAsync(string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId)
    {
        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        _db.Konton.Add(new Konto
        {
            Namn = namn,
            KontoNr = NullIfEmpty(kontoNr),
            Beskrivning = NullIfEmpty(beskrivning),
            KontotypId = kontotypId,
            InstitutId = institutId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateKontoAsync(int id, string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId)
    {
        var konto = await _db.Konton.FindAsync(id);
        if (konto is null)
        {
            return (false, "Kontot hittades inte.");
        }

        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        konto.Namn = namn;
        konto.KontoNr = NullIfEmpty(kontoNr);
        konto.Beskrivning = NullIfEmpty(beskrivning);
        konto.KontotypId = kontotypId;
        konto.InstitutId = institutId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteKontoAsync(int id)
    {
        var konto = await _db.Konton.FindAsync(id);
        if (konto is null)
        {
            return (false, "Kontot hittades inte.");
        }

        var harKopplingar = await _db.KontoAnvandare.AnyAsync(ka => ka.KontoId == id);
        if (harKopplingar)
        {
            return (false, $"Kontot \"{konto.Namn}\" har kopplade användare och kan inte tas bort. Ta bort kopplingarna först.");
        }

        _db.Konton.Remove(konto);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddKontoAnvandareAsync(int kontoId, int anvandareId, decimal andelProcent)
    {
        var alreadyLinked = await _db.KontoAnvandare.AnyAsync(ka => ka.KontoId == kontoId && ka.AnvandareId == anvandareId);
        if (alreadyLinked)
        {
            return (false, "Den här användaren är redan kopplad till kontot.");
        }

        _db.KontoAnvandare.Add(new KontoAnvandare { KontoId = kontoId, AnvandareId = anvandareId, AndelProcent = andelProcent });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveKontoAnvandareAsync(int linkId)
    {
        var link = await _db.KontoAnvandare.FindAsync(linkId);
        if (link is not null)
        {
            _db.KontoAnvandare.Remove(link);
            await _db.SaveChangesAsync();
        }
    }

    // --- Kategori ---

    public async Task<List<Kategori>> GetKategorierAsync(string? callerExtId, bool callerIsAdmin)
    {
        var query = _db.Kategorier
            .Include(k => k.Foralder)
            .Include(k => k.Ekonomi)
            .AsQueryable();

        if (!callerIsAdmin)
        {
            query = query.Where(k => k.EkonomiId != null &&
                (k.Ekonomi!.EkonomiAgare.ExtId == callerExtId ||
                 k.Ekonomi!.Anvandare.Any(ea => ea.Anvandare.ExtId == callerExtId)));
        }

        return await query.OrderBy(k => k.Namn).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateKategoriAsync(string? namn, string? beskrivning, int? foralderId, int? ekonomiId)
    {
        _db.Kategorier.Add(new Kategori
        {
            Namn = NullIfEmpty(namn),
            Beskrivning = NullIfEmpty(beskrivning),
            ForalderId = foralderId,
            EkonomiId = ekonomiId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateKategoriAsync(int id, string? namn, string? beskrivning, int? foralderId, int? ekonomiId)
    {
        var kategori = await _db.Kategorier.FindAsync(id);
        if (kategori is null)
        {
            return (false, "Kategorin hittades inte.");
        }

        if (foralderId == id)
        {
            return (false, "En kategori kan inte vara sin egen förälder.");
        }

        kategori.Namn = NullIfEmpty(namn);
        kategori.Beskrivning = NullIfEmpty(beskrivning);
        kategori.ForalderId = foralderId;
        kategori.EkonomiId = ekonomiId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteKategoriAsync(int id)
    {
        var kategori = await _db.Kategorier.FindAsync(id);
        if (kategori is null)
        {
            return (false, "Kategorin hittades inte.");
        }

        var harUnderkategorier = await _db.Kategorier.AnyAsync(k => k.ForalderId == id);
        if (harUnderkategorier)
        {
            return (false, $"Kategorin \"{kategori.Namn}\" har underkategorier och kan inte tas bort.");
        }

        _db.Kategorier.Remove(kategori);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    // --- Ekonomi ---

    public async Task<List<Ekonomi>> GetEkonomierAsync(string? callerExtId, bool callerIsAdmin)
    {
        var query = _db.Ekonomier
            .Include(e => e.EkonomiAgare)
            .Include(e => e.Anvandare).ThenInclude(ea => ea.Anvandare)
            .Include(e => e.Anvandare).ThenInclude(ea => ea.Anvandarroll)
            .AsQueryable();

        if (!callerIsAdmin)
        {
            query = query.Where(e => e.EkonomiAgare.ExtId == callerExtId ||
                e.Anvandare.Any(ea => ea.Anvandare.ExtId == callerExtId));
        }

        return await query.OrderBy(e => e.Namn).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateEkonomiAsync(string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId)
    {
        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        _db.Ekonomier.Add(new Ekonomi
        {
            Namn = namn,
            Beskrivning = NullIfEmpty(beskrivning),
            EkonomiAgareId = ekonomiAgareId,
            TransitKontoId = transitKontoId,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateEkonomiAsync(int id, string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId)
    {
        var ekonomi = await _db.Ekonomier.FindAsync(id);
        if (ekonomi is null)
        {
            return (false, "Ekonomin hittades inte.");
        }

        namn = namn.Trim();
        if (string.IsNullOrWhiteSpace(namn))
        {
            return (false, "Ange ett namn.");
        }

        ekonomi.Namn = namn;
        ekonomi.Beskrivning = NullIfEmpty(beskrivning);
        ekonomi.EkonomiAgareId = ekonomiAgareId;
        ekonomi.TransitKontoId = transitKontoId;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteEkonomiAsync(int id)
    {
        var ekonomi = await _db.Ekonomier.FindAsync(id);
        if (ekonomi is null)
        {
            return (false, "Ekonomin hittades inte.");
        }

        var harKopplingar = await _db.EkonomiAnvandare.AnyAsync(ea => ea.EkonomiId == id);
        var harKategorier = await _db.Kategorier.AnyAsync(k => k.EkonomiId == id);
        if (harKopplingar || harKategorier)
        {
            return (false, $"Ekonomin \"{ekonomi.Namn}\" har kopplade användare eller kategorier och kan inte tas bort.");
        }

        _db.Ekonomier.Remove(ekonomi);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddEkonomiAnvandareAsync(int ekonomiId, int anvandareId, int anvandarrollId, decimal andel)
    {
        var alreadyLinked = await _db.EkonomiAnvandare.AnyAsync(ea => ea.EkonomiId == ekonomiId && ea.AnvadareId == anvandareId);
        if (alreadyLinked)
        {
            return (false, "Den här användaren är redan kopplad till ekonomin.");
        }

        _db.EkonomiAnvandare.Add(new EkonomiAnvandare
        {
            EkonomiId = ekonomiId,
            AnvadareId = anvandareId,
            AnvandarrollId = anvandarrollId,
            Andel = andel,
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task RemoveEkonomiAnvandareAsync(int linkId)
    {
        var link = await _db.EkonomiAnvandare.FindAsync(linkId);
        if (link is not null)
        {
            _db.EkonomiAnvandare.Remove(link);
            await _db.SaveChangesAsync();
        }
    }

    // --- Transaktion ---

    private IQueryable<Transaktion> FilterTransaktionerByAccess(IQueryable<Transaktion> query, string? callerExtId, bool callerIsAdmin)
    {
        if (callerIsAdmin)
        {
            return query;
        }

        return query.Where(t => t.Ekonomi.EkonomiAgare.ExtId == callerExtId ||
            t.Ekonomi.Anvandare.Any(ea => ea.Anvandare.ExtId == callerExtId));
    }

    public async Task<List<Transaktion>> GetTransaktionerAsync(string? callerExtId, bool callerIsAdmin, int? ar, int? manad)
    {
        var query = _db.Transaktioner
            .Include(t => t.Datum)
            .Include(t => t.FranKonto)
            .Include(t => t.TillKonto)
            .Include(t => t.Kategori)
            .Include(t => t.Ekonomi)
            .Include(t => t.Anvandare)
            .AsQueryable();

        query = FilterTransaktionerByAccess(query, callerExtId, callerIsAdmin);

        if (ar.HasValue)
        {
            query = query.Where(t => t.Datum.Ar == ar.Value);
        }
        if (manad.HasValue)
        {
            query = query.Where(t => t.Datum.Manad == manad.Value);
        }

        return await query
            .OrderByDescending(t => t.Datum.Kalenderdatum)
            .ThenByDescending(t => t.Timestamp)
            .ToListAsync();
    }

    public async Task<List<int>> GetTransaktionArAsync(string? callerExtId, bool callerIsAdmin)
    {
        var query = FilterTransaktionerByAccess(_db.Transaktioner.Include(t => t.Datum), callerExtId, callerIsAdmin);

        return await query
            .Where(t => t.Datum.Ar != null)
            .Select(t => t.Datum.Ar!.Value)
            .Distinct()
            .OrderByDescending(a => a)
            .ToListAsync();
    }

    public async Task<List<int>> GetTransaktionManaderAsync(string? callerExtId, bool callerIsAdmin, int ar)
    {
        var query = FilterTransaktionerByAccess(_db.Transaktioner.Include(t => t.Datum), callerExtId, callerIsAdmin);

        return await query
            .Where(t => t.Datum.Ar == ar && t.Datum.Manad != null)
            .Select(t => t.Datum.Manad!.Value)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<(int Ar, int Manad)?> GetSenasteTransaktionsperiodAsync(string? callerExtId, bool callerIsAdmin)
    {
        var query = FilterTransaktionerByAccess(_db.Transaktioner.Include(t => t.Datum), callerExtId, callerIsAdmin);

        var senaste = await query
            .OrderByDescending(t => t.Datum.Kalenderdatum)
            .Select(t => new { t.Datum.Ar, t.Datum.Manad })
            .FirstOrDefaultAsync();

        if (senaste is null || senaste.Ar is null || senaste.Manad is null)
        {
            return null;
        }

        return (senaste.Ar.Value, senaste.Manad.Value);
    }

    public async Task<(bool Success, string? Error)> CreateTransaktionAsync(
        DateOnly datum, int franKontoId, int tillKontoId, int kategoriId, int ekonomiId,
        int? anvandareId, decimal belopp, bool aterkommande, string? kommentar,
        string? callerExtId, bool callerIsAdmin)
    {
        var datumRad = await _db.Datum.FirstOrDefaultAsync(d => d.Kalenderdatum == datum);
        if (datumRad is null)
        {
            return (false, "Ogiltigt datum.");
        }

        int resolvedAnvandareId;
        if (callerIsAdmin && anvandareId.HasValue)
        {
            resolvedAnvandareId = anvandareId.Value;
        }
        else
        {
            var caller = await _db.Anvandare.FirstOrDefaultAsync(a => a.ExtId == callerExtId);
            if (caller is null)
            {
                return (false, "Din användare hittades inte i EkoWeb.");
            }
            resolvedAnvandareId = caller.Id;
        }

        if (!callerIsAdmin)
        {
            var harTillgang = await _db.Ekonomier.AnyAsync(e => e.Id == ekonomiId &&
                (e.EkonomiAgare.ExtId == callerExtId || e.Anvandare.Any(ea => ea.Anvandare.ExtId == callerExtId)));
            if (!harTillgang)
            {
                return (false, "Du har inte behörighet till den ekonomin.");
            }
        }

        _db.Transaktioner.Add(new Transaktion
        {
            Timestamp = DateTime.UtcNow,
            DatumId = datumRad.Id,
            FranKontoId = franKontoId,
            TillKontoId = tillKontoId,
            KategoriId = kategoriId,
            EkonomiId = ekonomiId,
            AnvandareId = resolvedAnvandareId,
            Belopp = belopp,
            Aterkommande = aterkommande,
            Kommentar = NullIfEmpty(kommentar),
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateTransaktionAsync(
        int id, DateOnly datum, int franKontoId, int tillKontoId, int kategoriId, int ekonomiId,
        int anvandareId, decimal belopp, bool aterkommande, string? kommentar)
    {
        var transaktion = await _db.Transaktioner.FindAsync(id);
        if (transaktion is null)
        {
            return (false, "Transaktionen hittades inte.");
        }

        var datumRad = await _db.Datum.FirstOrDefaultAsync(d => d.Kalenderdatum == datum);
        if (datumRad is null)
        {
            return (false, "Ogiltigt datum.");
        }

        transaktion.DatumId = datumRad.Id;
        transaktion.FranKontoId = franKontoId;
        transaktion.TillKontoId = tillKontoId;
        transaktion.KategoriId = kategoriId;
        transaktion.EkonomiId = ekonomiId;
        transaktion.AnvandareId = anvandareId;
        transaktion.Belopp = belopp;
        transaktion.Aterkommande = aterkommande;
        transaktion.Kommentar = NullIfEmpty(kommentar);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteTransaktionAsync(int id)
    {
        var transaktion = await _db.Transaktioner.FindAsync(id);
        if (transaktion is null)
        {
            return (false, "Transaktionen hittades inte.");
        }

        _db.Transaktioner.Remove(transaktion);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
