using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Data.Entities;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/institut")]
public class InstitutController : ControllerBase
{
    private readonly IEkoWebService _service;

    public InstitutController(IEkoWebService service)
    {
        _service = service;
    }

    private static InstitutDto ToDto(Institut i) => new(i.Id, i.Namn, i.Beskrivning);

    [HttpGet]
    public async Task<ActionResult<List<InstitutDto>>> GetAll()
        => Ok((await _service.GetInstitutAsync()).Select(ToDto));

    [HttpPost]
    public async Task<IActionResult> Create(InstitutRequest request)
    {
        var (success, error) = await _service.CreateInstitutAsync(request.Namn, request.Beskrivning);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, InstitutRequest request)
    {
        var (success, error) = await _service.UpdateInstitutAsync(id, request.Namn, request.Beskrivning);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _service.DeleteInstitutAsync(id);
        return success ? Ok() : BadRequest(new { error });
    }
}
