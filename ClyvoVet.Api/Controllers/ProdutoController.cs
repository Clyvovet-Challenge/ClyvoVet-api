using ClyvoVet.Api.DTOs.Request;
using ClyvoVet.Api.Enums;
using ClyvoVet.Api.Filters;
using ClyvoVet.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVet.Api.Controllers;

/// <summary>
/// Gerencia o catálogo de produtos e serviços veterinários.
/// Tabela: <c>t_clyvo_produto</c>
/// </summary>
[ApiController]
[Route("api/v1/produtos")]
[Produces("application/json")]
[TypeFilter(typeof(ApiKeyFilterAttribute), Arguments = new object[] { "Api:ApiKey" })]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoService _service;

    public ProdutoController(IProdutoService service) => _service = service;

    /// <summary>Lista produtos com paginação e filtros opcionais.</summary>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Itens por página — máx. 100 (padrão: 10).</param>
    /// <param name="categoria">Filtro por categoria: <c>Racao | Medicamento | Acessorio | Servico | Outro</c></param>
    /// <param name="especieIndicada">Filtro por espécie: <c>Cachorro | Gato | Passaro | Reptil | Roedor | Todos | Outro | Bovino | Equino</c></param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] CategoriaEnum? categoria = null,
        [FromQuery] EspecieEnum? especieIndicada = null)
    {
        if (page < 1)
            return BadRequest(new { error = "O parâmetro 'page' deve ser maior que zero." });
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { error = "O parâmetro 'pageSize' deve estar entre 1 e 100." });

        var result = await _service.GetAllAsync(page, pageSize, categoria, especieIndicada);
        return Ok(result);
    }

    /// <summary>Retorna um produto pelo ID (UUID).</summary>
    /// <param name="id">UUID do produto.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo produto.
    /// O campo <c>id</c> é gerado automaticamente pelo Oracle (<c>fn_uuid()</c>).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ProdutoRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Atualiza um produto existente.</summary>
    /// <param name="id">UUID do produto a atualizar.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] ProdutoRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Remove um produto pelo ID.</summary>
    /// <param name="id">UUID do produto a remover.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
