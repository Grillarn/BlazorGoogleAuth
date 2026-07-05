using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/konton")]
public class KontonController : ControllerBase
{
    private readonly IEkoWebService _service;

    public KontonController(IEkoWebService service)
    {
        _service = service;
    }

    private static AnvandareDto ToDto(Anvandare a) => new(a.Id, a.ExtId, a.Mail, a.Fornamn, a.Efternamn);

    private static KontoDto ToDto(Konto k) => new(
        k.Id,
        k.Namn,
        k.KontoNr,
        k.Beskrivning,
        k.KontotypId,
        new KontotypDto(k.Kontotyp.Id, k.Kontotyp.Namn, k.Kontotyp.Externt),
        k.InstitutId,
        new InstitutDto(k.Institut.Id, k.Institut.Namn, k.Institut.Beskrivning),
        k.Anvandare.Select(a => new KontoAnvandareDto(a.Id, a.AnvandareId, ToDto(a.Anvandare), a.AndelProcent)).ToList());

    /// <summary>
    /// BlazorGoogleAuth skickar med vem som anropar (satt utifrån den inloggade
    /// användarens claims) i två headers, så vi kan filtrera/begränsa per person
    /// utan att ha ett eget inloggningssystem i det här API:et.
    /// </summary>
    private bool CallerIsAdmin
        => bool.TryParse(Request.Headers["X-Caller-Is-Admin"], out var isAdmin) && isAdmin;

    private string? CallerExtId
        => Request.Headers.TryGetValue("X-Caller-Ext-Id", out var value) ? value.ToString() : null;

    [HttpGet]
    public async Task<ActionResult<List<KontoDto>>> GetAll()
        => Ok((await _service.GetKontonAsync(CallerExtId, CallerIsAdmin)).Select(ToDto));

    [HttpPost]
    public async Task<IActionResult> Create(KontoRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.CreateKontoAsync(request.Namn, request.KontoNr, request.Beskrivning, request.KontotypId, request.InstitutId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, KontoRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.UpdateKontoAsync(id, request.Namn, request.KontoNr, request.Beskrivning, request.KontotypId, request.InstitutId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.DeleteKontoAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("{id:int}/anvandare")]
    public async Task<IActionResult> AddAnvandare(int id, KontoAnvandareRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.AddKontoAnvandareAsync(id, request.AnvandareId, request.AndelProcent);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("anvandare/{linkId:int}")]
    public async Task<IActionResult> RemoveAnvandare(int linkId)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        await _service.RemoveKontoAnvandareAsync(linkId);
        return Ok();
    }
}

