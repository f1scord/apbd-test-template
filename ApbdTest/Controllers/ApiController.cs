using ApbdTest.DTOs;
using ApbdTest.Exceptions;
using ApbdTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApbdTest.Controllers;

// TODO: rename to match exam entity, e.g. CustomersController → [Route("api/customers")]
[Route("api/[controller]")]
[ApiController]
public class MainController : ControllerBase
{
    private readonly IDbService _dbService;

    public MainController(IDbService dbService)
    {
        _dbService = dbService;
    }

    // TODO: adjust route, e.g. [Route("{id}/rentals")]
    [Route("{id}")]
    [HttpGet]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        try
        {
            var result = await _dbService.GetAsync(id);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    // TODO: adjust route if needed
    [Route("{id}")]
    [HttpPost]
    public async Task<IActionResult> Post([FromRoute] int id, [FromBody] CreateRootDto dto)
    {
        if (!dto.Items.Any())
        {
            return BadRequest("At least one item is required.");
        }

        try
        {
            await _dbService.CreateAsync(id, dto);
            return Created($"api/main/{id}", dto);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
