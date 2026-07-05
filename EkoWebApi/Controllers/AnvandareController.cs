using Microsoft.AspNetCore.Mvc;
using EkoWebApi.Dtos;
using EkoWebApi.Services;

namespace EkoWebApi.Controllers;

[ApiController]
[Route("api/anvandare")]
public class AnvandareController : ControllerBase
{
    private readonly IEkoWebService _service;

    public AnvandareController(IEkoWebService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AnvandareDto>>> GetAll()
        => Ok((await _service.GetAnvandareAsync()).Select(a => new AnvandareDto(a.Id, a.ExtId, a.Mail, a.Fornamn, a.Efternamn)));
}
