using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/transaktioner")]
public class TransaktionerController : ControllerBase
{
    private readonly IEkoWebService _service;

    public TransaktionerController(IEkoWebService service)
    {
        _service = service;
    }

    private static AnvandareDto ToDto(Anvandare a) => new(a.Id, a.ExtId, a.Mail, a.Fornamn, a.Efternamn);

    private static NamedRefDto ToRef(Konto k) => new(k.Id, k.Namn);

    private static NamedRefDto ToRef(Kategori k) => new(k.Id, k.Namn);

    private static NamedRefDto ToRef(Ekonomi e) => new(e.Id, e.Namn);

    private static TransaktionDto ToDto(Transaktion t) => new(
        t.Id,
        t.Datum.Kalenderdatum,
        t.Datum.Ar ?? 0,
        t.Datum.Manad ?? 0,
        t.FranKontoId,
        ToRef(t.FranKonto),
        t.TillKontoId,
        ToRef(t.TillKonto),
        t.KategoriId,
        ToRef(t.Kategori),
        t.EkonomiId,
        ToRef(t.Ekonomi),
        t.AnvandareId,
        ToDto(t.Anvandare),
        t.Belopp,
        t.Aterkommande,
        t.Kommentar);

    private bool CallerIsAdmin
        => bool.TryParse(Request.Headers["X-Caller-Is-Admin"], out var isAdmin) && isAdmin;

    private string? CallerExtId
        => Request.Headers.TryGetValue("X-Caller-Ext-Id", out var value) ? value.ToString() : null;

    [HttpGet]
    public async Task<ActionResult<List<TransaktionDto>>> GetAll([FromQuery] int? ar, [FromQuery] int? manad)
        => Ok((await _service.GetTransaktionerAsync(CallerExtId, CallerIsAdmin, ar, manad)).Select(ToDto));

    [HttpGet("ar")]
    public async Task<ActionResult<List<int>>> GetAr()
        => Ok(await _service.GetTransaktionArAsync(CallerExtId, CallerIsAdmin));

    [HttpGet("manader")]
    public async Task<ActionResult<List<int>>> GetManader([FromQuery] int ar)
        => Ok(await _service.GetTransaktionManaderAsync(CallerExtId, CallerIsAdmin, ar));

    [HttpGet("senaste-period")]
    public async Task<ActionResult<SenastePeriodResponse>> GetSenastePeriod()
    {
        var period = await _service.GetSenasteTransaktionsperiodAsync(CallerExtId, CallerIsAdmin);
        return Ok(new SenastePeriodResponse(period.HasValue ? new SenastePeriodDto(period.Value.Ar, period.Value.Manad) : null));
    }

    [HttpPost]
    public async Task<IActionResult> Create(TransaktionRequest request)
    {
        var (success, error) = await _service.CreateTransaktionAsync(
            request.Datum, request.FranKontoId, request.TillKontoId, request.KategoriId, request.EkonomiId,
            request.AnvandareId, request.Belopp, request.Aterkommande, request.Kommentar,
            CallerExtId, CallerIsAdmin);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TransaktionRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.UpdateTransaktionAsync(
            id, request.Datum, request.FranKontoId, request.TillKontoId, request.KategoriId, request.EkonomiId,
            request.AnvandareId ?? 0, request.Belopp, request.Aterkommande, request.Kommentar);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.DeleteTransaktionAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }
}
