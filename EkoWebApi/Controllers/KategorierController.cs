using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/kategorier")]
public class KategorierController : ControllerBase
{
    private readonly IEkoWebService _service;

    public KategorierController(IEkoWebService service)
    {
        _service = service;
    }

    private static KategoriDto ToDto(Kategori k) => new(
        k.Id,
        k.Namn,
        k.Beskrivning,
        k.ForalderId,
        k.Foralder is null ? null : new NamedRefDto(k.Foralder.Id, k.Foralder.Namn),
        k.EkonomiId,
        k.Ekonomi is null ? null : new NamedRefDto(k.Ekonomi.Id, k.Ekonomi.Namn));

    private bool CallerIsAdmin
        => bool.TryParse(Request.Headers["X-Caller-Is-Admin"], out var isAdmin) && isAdmin;

    private string? CallerExtId
        => Request.Headers.TryGetValue("X-Caller-Ext-Id", out var value) ? value.ToString() : null;

    [HttpGet]
    public async Task<ActionResult<List<KategoriDto>>> GetAll()
        => Ok((await _service.GetKategorierAsync(CallerExtId, CallerIsAdmin)).Select(ToDto));

    [HttpPost]
    public async Task<IActionResult> Create(KategoriRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.CreateKategoriAsync(request.Namn, request.Beskrivning, request.ForalderId, request.EkonomiId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, KategoriRequest request)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.UpdateKategoriAsync(id, request.Namn, request.Beskrivning, request.ForalderId, request.EkonomiId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CallerIsAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Kräver Admin." });
        }

        var (success, error) = await _service.DeleteKategoriAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }
}
