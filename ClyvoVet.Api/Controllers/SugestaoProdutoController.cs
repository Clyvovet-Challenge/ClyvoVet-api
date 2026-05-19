using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

[ApiController]
[Route("api/v1/sugestoes-produto")]
public class SugestaoProdutoController : ControllerBase
{
    private readonly ISugestaoProdutoService _service;

    public SugestaoProdutoController(ISugestaoProdutoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null)
    {
        if (page < 1) return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100) return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        var result = await _service.GetAllAsync(page, pageSize, animalId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SugestaoProdutoRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SugestaoProdutoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
