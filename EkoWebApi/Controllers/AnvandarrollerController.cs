using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/anvandarroller")]
public class AnvandarrollerController : ControllerBase
{
    private readonly IEkoWebService _service;

    public AnvandarrollerController(IEkoWebService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnvandarrollDto>>> GetAll()
        => Ok((await _service.GetAnvandarrollerAsync()).Select(r => new AnvandarrollDto(r.Id, r.Namn, r.Beskrivning)));
}
