using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorGoogleAuth.Services.EkoWeb;

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
    Task<List<Konto>> GetKontonAsync();
    Task<(bool Success, string? Error)> CreateKontoAsync(string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId);
    Task<(bool Success, string? Error)> UpdateKontoAsync(int id, string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId);
    Task<(bool Success, string? Error)> DeleteKontoAsync(int id);
    Task<(bool Success, string? Error)> AddKontoAnvandareAsync(int kontoId, int anvandareId, decimal andelProcent);
    Task RemoveKontoAnvandareAsync(int linkId);

    // --- Kategori ---
    Task<List<Kategori>> GetKategorierAsync();
    Task<(bool Success, string? Error)> CreateKategoriAsync(string? namn, string? beskrivning, int? foralderId, int? ekonomiId);
    Task<(bool Success, string? Error)> UpdateKategoriAsync(int id, string? namn, string? beskrivning, int? foralderId, int? ekonomiId);
    Task<(bool Success, string? Error)> DeleteKategoriAsync(int id);

    // --- Ekonomi (inkl. koppling till användare via Ekonomi_Anvandare) ---
    Task<List<Ekonomi>> GetEkonomierAsync();
    Task<(bool Success, string? Error)> CreateEkonomiAsync(string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId);
    Task<(bool Success, string? Error)> UpdateEkonomiAsync(int id, string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId);
    Task<(bool Success, string? Error)> DeleteEkonomiAsync(int id);
    Task<(bool Success, string? Error)> AddEkonomiAnvandareAsync(int ekonomiId, int anvandareId, int anvandarrollId, decimal andel);
    Task RemoveEkonomiAnvandareAsync(int linkId);
}

/// <summary>
/// Pratar med EkoWebApi via HTTP istället för att gå direkt mot EkoWeb-databasen.
/// All affärslogik (validering, "finns redan", "används fortfarande" osv.) ligger
/// i API:et - den här klassen är bara ett tunt HTTP-lager.
/// </summary>
public class EkoWebApiClient : IEkoWebService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;

    public EkoWebApiClient(HttpClient http, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    /// <summary>
    /// Konto-uppgifter är personliga (en vanlig användare ska bara se sina egna
    /// kopplade konton), så API:et behöver veta vem som frågar. Vi skickar med
    /// den inloggade användarens Google-id och adminstatus i headers - API:et
    /// litar på det eftersom det bara är BlazorGoogleAuth som anropar det
    /// (skyddat av den delade API-nyckeln).
    /// </summary>
    private async Task SetCallerHeadersAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var extId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var isAdmin = user.IsInRole("Admin");

        _http.DefaultRequestHeaders.Remove("X-Caller-Ext-Id");
        _http.DefaultRequestHeaders.Remove("X-Caller-Is-Admin");
        _http.DefaultRequestHeaders.Add("X-Caller-Ext-Id", extId);
        _http.DefaultRequestHeaders.Add("X-Caller-Is-Admin", isAdmin.ToString());
    }

    private static async Task<(bool Success, string? Error)> ToResult(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return (false, body?.Error ?? $"Anropet misslyckades ({(int)response.StatusCode}).");
        }
        catch
        {
            return (false, $"Anropet misslyckades ({(int)response.StatusCode}).");
        }
    }

    private record ErrorBody(string? Error);

    // --- Läs-uppslag ---

    public async Task<List<Anvandare>> GetAnvandareAsync()
        => await _http.GetFromJsonAsync<List<Anvandare>>("api/anvandare") ?? [];

    public async Task<List<Anvandarroll>> GetAnvandarrollerAsync()
        => await _http.GetFromJsonAsync<List<Anvandarroll>>("api/anvandarroller") ?? [];

    // --- Institut ---

    public async Task<List<Institut>> GetInstitutAsync()
        => await _http.GetFromJsonAsync<List<Institut>>("api/institut") ?? [];

    public async Task<(bool Success, string? Error)> CreateInstitutAsync(string namn, string? beskrivning)
        => await ToResult(await _http.PostAsJsonAsync("api/institut", new { Namn = namn, Beskrivning = beskrivning }));

    public async Task<(bool Success, string? Error)> UpdateInstitutAsync(int id, string namn, string? beskrivning)
        => await ToResult(await _http.PutAsJsonAsync($"api/institut/{id}", new { Namn = namn, Beskrivning = beskrivning }));

    public async Task<(bool Success, string? Error)> DeleteInstitutAsync(int id)
        => await ToResult(await _http.DeleteAsync($"api/institut/{id}"));

    // --- Kontotyp ---

    public async Task<List<Kontotyp>> GetKontotyperAsync()
        => await _http.GetFromJsonAsync<List<Kontotyp>>("api/kontotyper") ?? [];

    public async Task<(bool Success, string? Error)> CreateKontotypAsync(string namn, bool externt)
        => await ToResult(await _http.PostAsJsonAsync("api/kontotyper", new { Namn = namn, Externt = externt }));

    public async Task<(bool Success, string? Error)> UpdateKontotypAsync(int id, string namn, bool externt)
        => await ToResult(await _http.PutAsJsonAsync($"api/kontotyper/{id}", new { Namn = namn, Externt = externt }));

    public async Task<(bool Success, string? Error)> DeleteKontotypAsync(int id)
        => await ToResult(await _http.DeleteAsync($"api/kontotyper/{id}"));

    // --- Konto ---

    public async Task<List<Konto>> GetKontonAsync()
    {
        await SetCallerHeadersAsync();
        return await _http.GetFromJsonAsync<List<Konto>>("api/konton") ?? [];
    }

    public async Task<(bool Success, string? Error)> CreateKontoAsync(string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PostAsJsonAsync("api/konton", new { Namn = namn, KontoNr = kontoNr, Beskrivning = beskrivning, KontotypId = kontotypId, InstitutId = institutId }));
    }

    public async Task<(bool Success, string? Error)> UpdateKontoAsync(int id, string namn, string? kontoNr, string? beskrivning, int kontotypId, int institutId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PutAsJsonAsync($"api/konton/{id}", new { Namn = namn, KontoNr = kontoNr, Beskrivning = beskrivning, KontotypId = kontotypId, InstitutId = institutId }));
    }

    public async Task<(bool Success, string? Error)> DeleteKontoAsync(int id)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.DeleteAsync($"api/konton/{id}"));
    }

    public async Task<(bool Success, string? Error)> AddKontoAnvandareAsync(int kontoId, int anvandareId, decimal andelProcent)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PostAsJsonAsync($"api/konton/{kontoId}/anvandare", new { AnvandareId = anvandareId, AndelProcent = andelProcent }));
    }

    public async Task RemoveKontoAnvandareAsync(int linkId)
    {
        await SetCallerHeadersAsync();
        await _http.DeleteAsync($"api/konton/anvandare/{linkId}");
    }

    // --- Kategori ---

    public async Task<List<Kategori>> GetKategorierAsync()
    {
        await SetCallerHeadersAsync();
        return await _http.GetFromJsonAsync<List<Kategori>>("api/kategorier") ?? [];
    }

    public async Task<(bool Success, string? Error)> CreateKategoriAsync(string? namn, string? beskrivning, int? foralderId, int? ekonomiId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PostAsJsonAsync("api/kategorier", new { Namn = namn, Beskrivning = beskrivning, ForalderId = foralderId, EkonomiId = ekonomiId }));
    }

    public async Task<(bool Success, string? Error)> UpdateKategoriAsync(int id, string? namn, string? beskrivning, int? foralderId, int? ekonomiId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PutAsJsonAsync($"api/kategorier/{id}", new { Namn = namn, Beskrivning = beskrivning, ForalderId = foralderId, EkonomiId = ekonomiId }));
    }

    public async Task<(bool Success, string? Error)> DeleteKategoriAsync(int id)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.DeleteAsync($"api/kategorier/{id}"));
    }

    // --- Ekonomi ---

    public async Task<List<Ekonomi>> GetEkonomierAsync()
    {
        await SetCallerHeadersAsync();
        return await _http.GetFromJsonAsync<List<Ekonomi>>("api/ekonomier") ?? [];
    }

    public async Task<(bool Success, string? Error)> CreateEkonomiAsync(string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PostAsJsonAsync("api/ekonomier", new { Namn = namn, Beskrivning = beskrivning, EkonomiAgareId = ekonomiAgareId, TransitKontoId = transitKontoId }));
    }

    public async Task<(bool Success, string? Error)> UpdateEkonomiAsync(int id, string namn, string? beskrivning, int ekonomiAgareId, int? transitKontoId)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PutAsJsonAsync($"api/ekonomier/{id}", new { Namn = namn, Beskrivning = beskrivning, EkonomiAgareId = ekonomiAgareId, TransitKontoId = transitKontoId }));
    }

    public async Task<(bool Success, string? Error)> DeleteEkonomiAsync(int id)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.DeleteAsync($"api/ekonomier/{id}"));
    }

    public async Task<(bool Success, string? Error)> AddEkonomiAnvandareAsync(int ekonomiId, int anvandareId, int anvandarrollId, decimal andel)
    {
        await SetCallerHeadersAsync();
        return await ToResult(await _http.PostAsJsonAsync($"api/ekonomier/{ekonomiId}/anvandare", new { AnvandareId = anvandareId, AnvandarrollId = anvandarrollId, Andel = andel }));
    }

    public async Task RemoveEkonomiAnvandareAsync(int linkId)
    {
        await SetCallerHeadersAsync();
        await _http.DeleteAsync($"api/ekonomier/anvandare/{linkId}");
    }
}
