using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/kontotyper")]
public class KontotyperController : ControllerBase
{
    private readonly IEkoWebService _service;

    public KontotyperController(IEkoWebService service)
    {
        _service = service;
    }

    private static KontotypDto ToDto(Kontotyp k) => new(k.Id, k.Namn, k.Externt);

    [HttpGet]
    public async Task<ActionResult<List<KontotypDto>>> GetAll()
        => Ok((await _service.GetKontotyperAsync()).Select(ToDto));

    [HttpPost]
    public async Task<IActionResult> Create(KontotypRequest request)
    {
        var (success, error) = await _service.CreateKontotypAsync(request.Namn, request.Externt);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, KontotypRequest request)
    {
        var (success, error) = await _service.UpdateKontotypAsync(id, request.Namn, request.Externt);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _service.DeleteKontotypAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }
}
