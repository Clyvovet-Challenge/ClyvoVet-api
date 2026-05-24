using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia eventos públicos para pets (feiras, vacinações, workshops etc.).
/// Tabela: <c>t_clyvo_evento_pet</c>
/// </summary>
[ApiController]
[Route("api/v1/eventos-pet")]
[Produces("application/json")]
public class EventoPetController : ControllerBase
{
    private readonly IEventoPetService _service;

    public EventoPetController(IEventoPetService service) => _service = service;

    /// <summary>Lista eventos pet com paginação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="cidade">Filtro por cidade (case-insensitive).</param>
    /// <param name="tipo">Filtro: <c>Vacinacao | Feira | Castracao | Workshop | Outro</c></param>
    /// <param name="especieAlvo">Filtro: <c>Cachorro | Gato | Passaro | Reptil | Roedor | Todos | Outro</c></param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? cidade = null,
        [FromQuery] TipoEventoPetEnum? tipo = null,
        [FromQuery] EspecieEnum? especieAlvo = null)
    {
        if (page < 1)
            return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        var result = await _service.GetAllAsync(page, pageSize, cidade, tipo, especieAlvo);
        return Ok(result);
    }

    /// <summary>Retorna um evento pet pelo ID (UUID).</summary>
    /// <param name="id">UUID do evento.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo evento pet.
    /// O <c>id</c> é gerado pelo Oracle (<c>fn_uuid()</c>).
    /// <c>dataInicio</c> não pode ser no passado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EventoPetRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um evento pet existente.
    /// Eventos já iniciados podem ser editados; <c>dataInicio</c> só pode ser
    /// alterada para uma data futura.
    /// </summary>
    /// <param name="id">UUID do evento a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] EventoPetRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um evento pet pelo ID.</summary>
    /// <param name="id">UUID do evento a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
