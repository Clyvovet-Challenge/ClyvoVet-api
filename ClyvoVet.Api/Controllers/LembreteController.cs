using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia lembretes de cuidados vinculados a um animal.
/// Tabela: <c>t_clyvo_lembrete</c>
/// — requer <c>animal_id</c> válido em <c>t_clyvo_animal</c> (domínio Java).
/// </summary>
[ApiController]
[Route("api/v1/lembretes")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class LembreteController : ControllerBase
{
    private readonly ILembreteService _service;

    public LembreteController(ILembreteService service) => _service = service;

    /// <summary>Lista lembretes com paginação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="animalId">Filtra pelo UUID do animal.</param>
    /// <param name="status">Filtro: <c>Pendente | Enviado | Cancelado</c></param>
    /// <param name="tipo">Filtro: <c>Vacina | Medicamento | Consulta | Higiene | Outro</c></param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null,
        [FromQuery] StatusLembreteEnum? status = null,
        [FromQuery] TipoLembreteEnum? tipo = null)
    {
        if (page < 1)
            return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        var result = await _service.GetAllAsync(page, pageSize, animalId, status, tipo);
        return Ok(result);
    }

    /// <summary>Retorna um lembrete pelo ID (UUID).</summary>
    /// <param name="id">UUID do lembrete.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cria um novo lembrete.
    /// O <c>id</c> é gerado pelo Oracle (<c>fn_uuid()</c>).
    /// O campo <c>status</c> é forçado a <c>Pendente</c> independente do valor enviado.
    /// <c>agendadoEm</c> deve ser uma data/hora futura.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] LembreteRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Atualiza um lembrete existente.
    /// <c>agendadoEm</c> deve ser uma data/hora futura.
    /// </summary>
    /// <param name="id">UUID do lembrete a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] LembreteRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um lembrete pelo ID.</summary>
    /// <param name="id">UUID do lembrete a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
