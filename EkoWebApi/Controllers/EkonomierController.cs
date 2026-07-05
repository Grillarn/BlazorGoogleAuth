using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/ekonomier")]
public class EkonomierController : ControllerBase
{
    private readonly IEkoWebService _service;

    public EkonomierController(IEkoWebService service)
    {
        _service = service;
    }

    private static AnvandareDto ToDto(Anvandare a) => new(a.Id, a.ExtId, a.Mail, a.Fornamn, a.Efternamn);

    private static AnvandarrollDto ToDto(Anvandarroll r) => new(r.Id, r.Namn, r.Beskrivning);

    private static EkonomiDto ToDto(Ekonomi e) => new(
        e.Id,
        e.Namn,
        e.Beskrivning,
        e.EkonomiAgareId,
        ToDto(e.EkonomiAgare),
        e.TransitKontoId,
        e.Anvandare.Select(a => new EkonomiAnvandareDto(a.Id, a.AnvadareId, ToDto(a.Anvandare), a.AnvandarrollId, ToDto(a.Anvandarroll), a.Andel)).ToList());

    /// <summary>
    /// BlazorGoogleAuth skickar med vem som anropar i två headers, så vi kan
    /// filtrera/begränsa per person utan ett eget inloggningssystem här.
    /// </summary>
    private bool CallerIsAdmin
        => bool.TryParse(Request.Headers["X-Caller-Is-Admin"], out var isAdmin) && isAdmin;

    private string? CallerExtId
        => Request.Headers.TryGetValue("X-Caller-Ext-Id", out var value) ? value.ToString() : null;

    [HttpGet]
    public async Task<ActionResult<List<EkonomiDto>>> GetAll()
        => Ok((await _service.GetEkonomierAsync(CallerExtId, CallerIsAdmin)).Select(ToDto));

    [HttpPost]
    public async Task<IActionResult> Create(EkonomiRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.CreateEkonomiAsync(request.Namn, request.Beskrivning, request.EkonomiAgareId, request.TransitKontoId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EkonomiRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.UpdateEkonomiAsync(id, request.Namn, request.Beskrivning, request.EkonomiAgareId, request.TransitKontoId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.DeleteEkonomiAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("{id:int}/anvandare")]
    public async Task<IActionResult> AddAnvandare(int id, EkonomiAnvandareRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.AddEkonomiAnvandareAsync(id, request.AnvandareId, request.AnvandarrollId, request.Andel);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("anvandare/{linkId:int}")]
    public async Task<IActionResult> RemoveAnvandare(int linkId)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        await _service.RemoveEkonomiAnvandareAsync(linkId);
        return Ok();
    }
}
