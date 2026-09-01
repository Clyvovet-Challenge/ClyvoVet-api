using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia sugestões de produto vinculadas a um animal.
/// Tabela: <c>t_clyvo_sugestao_produto</c>
/// — requer <c>animal_id</c> válido em <c>t_clyvo_animal</c> e
///   <c>produto_id</c> válido em <c>t_clyvo_produto</c>.
/// </summary>
[ApiController]
[Route("api/v1/sugestoes-produto")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class SugestaoProdutoController : ControllerBase
{
    private readonly ISugestaoProdutoService _service;

    public SugestaoProdutoController(ISugestaoProdutoService service) => _service = service;

    /// <summary>Lista sugestões com paginação e filtro opcional por animal.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="animalId">Filtra pelo UUID do animal.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? animalId = null)
    {
        if (page < 1)
            return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        var result = await _service.GetAllAsync(page, pageSize, animalId);
        return Ok(result);
    }

    /// <summary>Retorna uma sugestão de produto pelo ID (UUID).</summary>
    /// <param name="id">UUID da sugestão.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova sugestão de produto.
    /// O <c>id</c> é gerado pelo Oracle (<c>fn_uuid()</c>).
    /// Valida a existência de <c>animalId</c> e <c>produtoId</c> antes de salvar.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] SugestaoProdutoRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza uma sugestão de produto existente.</summary>
    /// <param name="id">UUID da sugestão a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] SugestaoProdutoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove uma sugestão de produto pelo ID.</summary>
    /// <param name="id">UUID da sugestão a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
